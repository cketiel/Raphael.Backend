using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Raphael.Api.Services.Integration
{
    /// <summary>
    /// What an integrator is told about a rejected trip, and whether sending the same
    /// payload again could ever succeed.
    /// </summary>
    /// <param name="Code">Stable code from <see cref="IntegrationErrorCode"/>.</param>
    /// <param name="Message">Business-language explanation, safe to hand to a third party.</param>
    /// <param name="Retryable">True when the trip failed for a transient reason.</param>
    public sealed record IntegrationSyncError(string Code, string Message, bool Retryable);

    /// <summary>
    /// Turns a persistence exception into something an integrator can act on.
    /// </summary>
    /// <remarks>
    /// Two rules govern everything in this class.
    ///
    /// <para>
    /// <b>SQL Server error text carries data.</b> A duplicate key message ends in
    /// "The duplicate key value is (...)" and a truncation message quotes the value that
    /// did not fit. On this database those values are patient dates, addresses and phone
    /// numbers. So the raw message never reaches the response and never reaches the log
    /// either: only the error number and the constraint or column identifier are lifted
    /// out, and both of those are schema rather than data.
    /// </para>
    ///
    /// <para>
    /// <b>The integrator is outside the system.</b> They are told what is wrong with the
    /// trip they sent, in the vocabulary of the trip they sent. They are never told a
    /// table, a column, a constraint or a SQL error number, because that is a map of the
    /// database handed to someone who should not have one. When the cause cannot be
    /// stated safely they get a correlation id instead, which support exchanges for the
    /// full server-side record.
    /// </para>
    /// </remarks>
    public static class IntegrationErrorTranslator
    {
        // Duplicate-key text quotes the index as 'IX_Name'; foreign-key text quotes the
        // constraint as "FK_Name". Both spellings, and the bracketed form, in one pass.
        private static readonly Regex ConstraintName = new(
            @"(?:constraint|index)\s+['""\[]([^'""\]]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ColumnName = new(
            @"column\s+'([^']+)'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // SQL Server error numbers this endpoint can realistically provoke.
        private const int DuplicateKeyRow = 2601;
        private const int UniqueConstraintViolation = 2627;
        private const int ConstraintConflict = 547;   // foreign key or check
        private const int NullIntoNotNullColumn = 515;
        private const int TruncationLegacy = 8152;
        private const int TruncationWithColumn = 2628;
        private const int DeadlockVictim = 1205;
        private const int CommandTimeout = -2;

        private const string InternalMessage =
            "This trip could not be stored and nothing was saved for it. Quote the correlation id when you contact support.";

        /// <summary>
        /// Decides what to tell the integrator about a failed trip.
        /// </summary>
        public static IntegrationSyncError Translate(Exception exception)
        {
            // Already decided, and worded, by whoever refused the trip.
            if (exception is IntegrationRejectedException rejected)
            {
                return rejected.Error;
            }

            var sql = FindSqlException(exception);

            if (sql is null)
            {
                return exception is DbUpdateConcurrencyException
                    ? new IntegrationSyncError(
                        IntegrationErrorCode.ConcurrencyConflict,
                        "This trip was being modified by another operation at the same time and was not stored. Send it again.",
                        Retryable: true)
                    : new IntegrationSyncError(IntegrationErrorCode.Internal, InternalMessage, Retryable: false);
            }

            return sql.Number switch
            {
                DuplicateKeyRow or UniqueConstraintViolation => Duplicate(ExtractConstraint(sql)),

                ConstraintConflict => new IntegrationSyncError(
                    IntegrationErrorCode.InvalidReference,
                    "The trip refers to something this system does not have. Check SpaceTypeName (AMB, WCH or STR) and FundingSourceName.",
                    Retryable: false),

                NullIntoNotNullColumn => new IntegrationSyncError(
                    IntegrationErrorCode.MissingRequiredField,
                    "A field the trip cannot be stored without arrived empty. Check Date, CustomerFullName, PickupAddress, DropoffAddress, SpaceTypeName and FundingSourceName.",
                    Retryable: false),

                TruncationLegacy or TruncationWithColumn => new IntegrationSyncError(
                    IntegrationErrorCode.FieldTooLong,
                    "One of the text fields is longer than this system accepts. Pickup and dropoff addresses are limited to 450 characters.",
                    Retryable: false),

                DeadlockVictim => new IntegrationSyncError(
                    IntegrationErrorCode.ConcurrencyConflict,
                    "This trip collided with another operation running at the same time and was rolled back. Send it again.",
                    Retryable: true),

                CommandTimeout => new IntegrationSyncError(
                    IntegrationErrorCode.Timeout,
                    "Storing this trip took too long and was rolled back. Send it again.",
                    Retryable: true),

                _ => new IntegrationSyncError(IntegrationErrorCode.Internal, InternalMessage, Retryable: false)
            };
        }

        /// <summary>
        /// Names the uniqueness rule that rejected the trip, without naming the index.
        /// </summary>
        private static IntegrationSyncError Duplicate(string? constraint)
        {
            if (constraint is null)
            {
                return new IntegrationSyncError(
                    IntegrationErrorCode.DuplicateRecord,
                    "This trip conflicts with a record that already exists. Check the TripId and the patient identifiers you sent.",
                    Retryable: false);
            }

            // IX_Trip_Unique_Active_Trip covers date, patient, both addresses and the window.
            if (constraint.Contains("Trip_Unique_Active_Trip", StringComparison.OrdinalIgnoreCase))
            {
                return new IntegrationSyncError(
                    IntegrationErrorCode.DuplicateActiveTrip,
                    "An active trip already exists for this patient on the same date, with the same pickup and dropoff addresses and the same pickup window. "
                    + "Send an update using the TripId that trip was created with, or cancel it before creating a new one.",
                    Retryable: false);
            }

            if (constraint.Contains("RiderId", StringComparison.OrdinalIgnoreCase))
            {
                return new IntegrationSyncError(
                    IntegrationErrorCode.DuplicateRider,
                    "The RiderId you sent is already registered against a different patient. Send a RiderId that is unique to this patient, "
                    + "or omit it so the patient is matched by full name and phone.",
                    Retryable: false);
            }

            return new IntegrationSyncError(
                IntegrationErrorCode.DuplicateRecord,
                "This trip conflicts with a record that already exists. Check the TripId and the patient identifiers you sent.",
                Retryable: false);
        }

        /// <summary>
        /// Builds the one line about this failure that is safe to write to a log file.
        /// </summary>
        /// <remarks>
        /// Deliberately never returns <c>exception.Message</c> or <c>ToString()</c> for a
        /// database failure: on the paths that matter those strings quote the row that was
        /// rejected, which here is patient data. For a failure that is not the database
        /// talking the message is our own code and is kept, because otherwise a plain bug
        /// leaves nothing to debug with.
        /// </remarks>
        public static string DescribeForLog(Exception exception)
        {
            if (exception is IntegrationRejectedException rejected)
            {
                return $"Refused before persistence: {rejected.Error.Code}";
            }

            var sql = FindSqlException(exception);

            if (sql is null)
            {
                return $"{exception.GetType().FullName}: {exception.Message}";
            }

            var identifier = ExtractConstraint(sql) ?? ExtractColumn(sql) ?? "unidentified";
            return $"SqlException number={sql.Number} target={identifier} (message withheld: contains row data)";
        }

        private static SqlException? FindSqlException(Exception? exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqlException sql)
                {
                    return sql;
                }
            }

            return null;
        }

        private static string? ExtractConstraint(SqlException sql)
        {
            var match = ConstraintName.Match(sql.Message);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? ExtractColumn(SqlException sql)
        {
            var match = ColumnName.Match(sql.Message);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

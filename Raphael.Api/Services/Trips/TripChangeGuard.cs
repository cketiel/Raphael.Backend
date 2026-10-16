using Microsoft.AspNetCore.Http;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Trips
{
    /// <summary>The <c>code</c> a refused change carries in its 409, so a client can tell them apart.</summary>
    public static class TripLockCodes
    {
        /// <summary>The trip belongs to a past day, or is closed (Finished, Billed, Payed).</summary>
        public const string ReadOnly = "trip_read_only";

        /// <summary>An admin may correct a read-only trip, but only by saying why.</summary>
        public const string CorrectionReasonRequired = "correction_reason_required";

        /// <summary>The trip is on a route, and the fields asked for are copied onto its stops.</summary>
        public const string RoutedFieldsLocked = "trip_routed_locked";
    }

    /// <summary>
    /// A change the trip's state does not allow. Answered as a 409 by the global exception handler.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deliberately not an <see cref="InvalidOperationException"/>: several actions turn those
    /// into a 409 that reads "duplicate", and this is a different conversation with the user.
    /// </remarks>
    public sealed class TripLockedException : Exception
    {
        public TripLockedException(string code, string message, IReadOnlyList<string>? fields = null)
            : base(message)
        {
            Code = code;
            Fields = fields ?? Array.Empty<string>();
        }

        public string Code { get; }

        /// <summary>The fields that cannot change, for <see cref="TripLockCodes.RoutedFieldsLocked"/>.</summary>
        public IReadOnlyList<string> Fields { get; }
    }

    /// <summary>
    /// The one place that decides whether a trip, or a day of a route, may still be changed.
    /// </summary>
    /// <remarks>
    /// Two rules, and they are not the same rule:
    /// <list type="bullet">
    /// <item><b>Read-only</b> — a trip of a past day, or one already closed, is the record of what
    /// happened: reports and billing are built on it. Nobody changes it, except an admin who
    /// gives a reason, and that correction is written to <c>TripHistory</c>.</item>
    /// <item><b>Routed</b> — a trip on a route had its date, hours, addresses, type, patient and
    /// funding copied onto its two stops when it was routed. Changing them on the trip leaves
    /// the driver working from the old copy, so they are refused until the trip is taken back
    /// to the open list. No admin bypass: the fix is unrouting, not permission.</item>
    /// </list>
    /// "Past" is the trip's own operating day (<see cref="IOperationClock.TodayFor"/>), never the
    /// server's: a trip at 23:30 does not become history because the server is in another zone.
    /// </remarks>
    public interface ITripChangeGuard
    {
        /// <summary>True when the trip is of a past day or closed.</summary>
        bool IsReadOnly(Trip trip);

        /// <summary>True when the date is before today on the provider's clock.</summary>
        bool IsPastDay(DateTime date, int? providerId);

        /// <summary>
        /// Refuses a change to a read-only trip, unless an admin is correcting it. In that case the
        /// correction is added to the context, to be saved with the change itself.
        /// </summary>
        void EnsureTripWritable(Trip trip, string operation);

        /// <summary>
        /// The same rule for a route's stops, where only the day counts.
        /// </summary>
        /// <remarks>
        /// A Finished trip does not lock its stops on its own day: the dispatch screen re-chains
        /// the ETAs of the whole route every time a stop is completed, and those rows go too.
        /// </remarks>
        void EnsureDayWritable(DateTime date, int? providerId, int? tripId, string operation);

        /// <summary>Refuses the change when the trip is routed and any of these fields move.</summary>
        void EnsureRoutedFieldsUnchanged(Trip trip, IReadOnlyList<string> changedFields);
    }

    public sealed class TripChangeGuard : ITripChangeGuard
    {
        /// <summary>The header an admin's correction arrives with, URL-encoded so any language travels.</summary>
        public const string CorrectionReasonHeader = "X-Correction-Reason";

        private const int AdminRoleId = 1;
        private const int MaxReasonLength = 500;

        private static readonly HashSet<string> ClosedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            TripStatus.Finished, TripStatus.Billed, TripStatus.Payed
        };

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IOperationClock _clock;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<TripChangeGuard> _logger;

        public TripChangeGuard(RaphaelContext context, ICurrentUserService currentUser, IOperationClock clock,
            IHttpContextAccessor http, ILogger<TripChangeGuard> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _clock = clock;
            _http = http;
            _logger = logger;
        }

        public bool IsPastDay(DateTime date, int? providerId) =>
            date.Date < _clock.TodayFor(providerId).Date;

        public bool IsReadOnly(Trip trip) =>
            IsPastDay(trip.Date, trip.ProviderId) || (trip.Status != null && ClosedStatuses.Contains(trip.Status));

        public void EnsureTripWritable(Trip trip, string operation)
        {
            if (!IsReadOnly(trip)) return;

            var why = IsPastDay(trip.Date, trip.ProviderId)
                ? "This trip belongs to a past day and is read-only."
                : $"This trip is {trip.Status} and is read-only.";

            AllowCorrectionOrRefuse(trip.Id, why, operation, () => Snapshot(trip));
        }

        public void EnsureDayWritable(DateTime date, int? providerId, int? tripId, string operation)
        {
            if (!IsPastDay(date, providerId)) return;

            AllowCorrectionOrRefuse(tripId, "This route belongs to a past day and is read-only.", operation,
                () => $"Date={date:yyyy-MM-dd}");
        }

        public void EnsureRoutedFieldsUnchanged(Trip trip, IReadOnlyList<string> changedFields)
        {
            if (trip.VehicleRouteId == null || changedFields.Count == 0) return;

            throw new TripLockedException(
                TripLockCodes.RoutedFieldsLocked,
                "This trip is on a route, and these fields are copied onto its stops: " +
                string.Join(", ", changedFields) +
                ". Take the trip off the route (back to the open list) to change them.",
                changedFields);
        }

        private void AllowCorrectionOrRefuse(int? tripId, string why, string operation, Func<string> snapshot)
        {
            if (_currentUser.RoleId != AdminRoleId)
                throw new TripLockedException(TripLockCodes.ReadOnly,
                    why + " Past and closed trips are not changed, so that reports and billing keep telling the truth.");

            var reason = ReadReason();
            if (reason == null)
                throw new TripLockedException(TripLockCodes.CorrectionReasonRequired,
                    why + " As an admin you can correct it, but the reason for the correction is required.");

            var user = $"Admin - {_currentUser.UserName ?? "unknown"}";

            if (tripId is int id)
            {
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = id,
                    User = user,
                    Field = "Correction",
                    PriorValue = $"{operation}: {snapshot()}",
                    NewValue = reason,
                    ChangeDate = _clock.UtcNow
                });
            }
            else
            {
                // A garage event has no trip to hang a history row on. The reason may name a
                // patient, so it stays out of the log: who, what and when are enough to find it.
                _logger.LogWarning("Admin correction on a past route without a trip. User={User} Operation={Operation} {Snapshot}",
                    user, operation, snapshot());
            }
        }

        private string? ReadReason()
        {
            var raw = _http.HttpContext?.Request.Headers[CorrectionReasonHeader].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string text;
            try { text = Uri.UnescapeDataString(raw).Trim(); }
            catch (UriFormatException) { return null; }

            if (text.Length == 0) return null;
            return text.Length > MaxReasonLength ? text[..MaxReasonLength] : text;
        }

        private static string Snapshot(Trip t) =>
            $"Date={t.Date:yyyy-MM-dd}, FromTime={t.FromTime}, ToTime={t.ToTime}, Status={t.Status}, " +
            $"IsCancelled={t.IsCancelled}, WillCall={t.WillCall}, VehicleRouteId={t.VehicleRouteId}";
    }
}

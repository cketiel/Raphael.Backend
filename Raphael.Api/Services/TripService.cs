using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Integration;
using Raphael.Api.Services.Notifications;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Raphael.Api.Services
{
    public class TripService : ITripService
    {
        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly NotificationService _notificationService;
        private readonly ITripNotificationPublisher _tripNotifications;
        private readonly IOperationClock _clock;
        private readonly ILogger<TripService> _logger;

        public TripService(RaphaelContext context, ICurrentUserService currentUserService, NotificationService notificationService, ITripNotificationPublisher tripNotifications, IOperationClock clock, ILogger<TripService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _tripNotifications = tripNotifications;
            _clock = clock;
            _logger = logger;
        }

        /// <summary>
        /// The hour a Will Call carries until the patient says they are ready.
        /// </summary>
        /// <remarks>
        /// A convention, not a computation: 23:59 in a pickup time is how the whole office
        /// reads "this one is waiting on the patient".
        /// </remarks>
        private static readonly TimeSpan WillCallPickupTime = new(23, 59, 0);

        public async Task<List<string>> UpsertPortalTripsAsync(List<PortalTripDto> dtos, int? integratorId)
        {
            var processedIds = new List<string>();

            foreach (var dto in dtos)
            {
                // 1. Resolve SpaceType 
                var spaceType = await _context.SpaceTypes.FirstOrDefaultAsync(s => s.Name == dto.SpaceTypeName);
                if (spaceType == null)
                {
                    spaceType = new SpaceType
                    {
                        Name = dto.SpaceTypeName,
                        Description = "Auto-created via Portal",
                        LoadTime = 0,
                        UnloadTime = 0,
                        CapacityTypeId = 1,
                        IsActive = true
                    };
                    _context.SpaceTypes.Add(spaceType);
                    await _context.SaveChangesAsync();
                }

                // 2. Resolve FundingSource
                var fundingSource = await _context.FundingSources.FirstOrDefaultAsync(f => f.Name == dto.FundingSourceName);
                if (fundingSource == null)
                {
                    fundingSource = new FundingSource { Name = dto.FundingSourceName ?? "Unknown", IsActive = true };
                    _context.FundingSources.Add(fundingSource);
                    await _context.SaveChangesAsync();
                }

                // 3. Resolve Customer 
                string effectiveRiderId = string.IsNullOrWhiteSpace(dto.RiderId)
                    ? $"{dto.CustomerFullName} {dto.CustomerPhone}".Trim()
                    : dto.RiderId;

                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.RiderId == effectiveRiderId);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        RiderId = effectiveRiderId,
                        FullName = dto.CustomerFullName,
                        Phone = dto.CustomerPhone,
                        Address = dto.CustomerAddress ?? "Portal Provided",
                        City = dto.CustomerCity ?? "Unknown",
                        Zip = dto.CustomerZip ?? "00000",
                        State = "FL", // Default
                        Gender = dto.CustomerGender ?? "Unknown",
                        DOB = dto.CustomerDOB, 
                        FundingSourceId = fundingSource.Id,
                        SpaceTypeId = spaceType.Id,
                        Created = DateTime.UtcNow,
                        CreatedBy = "PortalUser",
                        IntegratorId = integratorId
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // 4. Procesar el Viaje Principal (Ida)
                var mainTripInternalId = await ProcessSingleTripAsync(dto, customer.Id, spaceType.Id, fundingSource.Id, integratorId, false);
                processedIds.Add(dto.TripId ?? mainTripInternalId.ToString());

                // 5. Si es Round Trip, procesar el Viaje de Regreso
                if (dto.IsRoundTrip && dto.ReturnTime.HasValue)
                {
                    // Creamos un DTO "espejo" para el regreso
                    var returnDto = new PortalTripDto
                    {
                        Date = dto.Date,
                        FromTime = dto.ReturnTime,
                        Type = TripType.Return,
                        PickupAddress = dto.DropoffAddress,
                        PickupLatitude = dto.DropoffLatitude,
                        PickupLongitude = dto.DropoffLongitude,
                        DropoffAddress = dto.PickupAddress,
                        DropoffLatitude = dto.PickupLatitude,
                        DropoffLongitude = dto.PickupLongitude,
                        PickupCity = dto.DropoffCity,
                        DropoffCity = dto.PickupCity,
                        Distance = dto.Distance,
                        Authorization = dto.Authorization,
                        Attachment = dto.Attachment, // We reuse the same file if it exists.
                        PickupComment = dto.RoundTripPickupComment,
                        DropoffComment = dto.RoundTripDropoffComment
                    };

                    await ProcessSingleTripAsync(returnDto, customer.Id, spaceType.Id, fundingSource.Id, integratorId, true);
                }
            }

            await _context.SaveChangesAsync();
            return processedIds;
        }

        // Private helper method to avoid duplicating mapping and attachment logic.
        private async Task<int> ProcessSingleTripAsync(PortalTripDto dto, int customerId, int spaceTypeId, int fundingSourceId, int? integratorId, bool isReturn)
        {
            // Check for existence using InternalId (Web) or TripId + IntegratorId (API).
            Trip? trip = null;
            if (dto.InternalId.HasValue)
                trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == dto.InternalId && t.IntegratorId == integratorId);

            if (trip == null && !string.IsNullOrEmpty(dto.TripId))
                trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == dto.TripId && t.IntegratorId == integratorId);

            if (trip == null)
            {
                trip = new Trip
                {
                    IntegratorId = integratorId,
                    Created = DateTime.UtcNow,
                    Status = TripStatus.Accepted
                };
                _context.Trips.Add(trip);
            }

            // Property Mapping 
            trip.Date = dto.Date;
            trip.Day = dto.Date.DayOfWeek.ToString();
            trip.FromTime = dto.FromTime;
            trip.ToTime = dto.ToTime;
            trip.CustomerId = customerId;
            trip.SpaceTypeId = spaceTypeId;
            trip.FundingSourceId = fundingSourceId;
            trip.PickupAddress = dto.PickupAddress;
            trip.PickupLatitude = dto.PickupLatitude;
            trip.PickupLongitude = dto.PickupLongitude;
            trip.DropoffAddress = dto.DropoffAddress;
            trip.DropoffLatitude = dto.DropoffLatitude;
            trip.DropoffLongitude = dto.DropoffLongitude;
            trip.Distance = dto.Distance;
            trip.PickupCity = dto.PickupCity;
            trip.DropoffCity = dto.DropoffCity;
            trip.Type = dto.Type ?? (isReturn ? TripType.Return : TripType.Appointment);
            trip.Authorization = dto.Authorization;
            trip.IsCancelled = false;
            trip.PickupComment = dto.PickupComment;
            trip.DropoffComment = dto.DropoffComment;

            await _context.SaveChangesAsync();

            // If there is no TripId (Manual), we assign the auto-generated ID.
            if (string.IsNullOrEmpty(trip.TripId))
            {
                trip.TripId = trip.Id.ToString();
                await _context.SaveChangesAsync();
            }

            // Attachment Logic 
            var existingAttachment = await _context.TripAttachments.FirstOrDefaultAsync(a => a.TripId == trip.Id);
            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                using var ms = new MemoryStream();
                await dto.Attachment.CopyToAsync(ms);
                byte[] fileData = ms.ToArray();

                if (existingAttachment == null)
                {
                    _context.TripAttachments.Add(new TripAttachment
                    {
                        TripId = trip.Id,
                        FileName = dto.Attachment.FileName,
                        FileContent = fileData,
                        ContentType = dto.Attachment.ContentType,
                        Created = DateTime.UtcNow
                    });
                }
                else
                {
                    existingAttachment.FileName = dto.Attachment.FileName;
                    existingAttachment.FileContent = fileData;
                    existingAttachment.ContentType = dto.Attachment.ContentType;
                    existingAttachment.Created = DateTime.UtcNow;
                }
            }

            return trip.Id;
        }

        /// <param name="cancelledBy">
        /// Which actor is cancelling. The same method serves the Booking Portal, where a
        /// facility acts through a JWT, and the Integration API, where an external system
        /// acts through its API Key. Both carry an IntegratorId, so it cannot be inferred:
        /// the patient must be told who dropped their ride.
        /// </param>
        public async Task<int> CancelIntegrationTripsAsync(List<string> externalTripIds, int? integratorId, string? integratorName, string cancelledBy = CancelledByTypes.Integrator)
        {
            var trips = await _context.Trips
                .Where(t => externalTripIds.Contains(t.TripId) && t.IntegratorId == integratorId)
                .ToListAsync();

            var cancelled = new List<(Trip Trip, string PreviousStatus)>();

            /*string user = !string.IsNullOrEmpty(integratorName) ? integratorName : "Unknown Integrator";
            user = $"Integrator - {user}";*/

            foreach (var trip in trips)
            {
                if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
                {
                    continue;
                }

                string priorValue = $"trip.Status={trip.Status}, trip.IsCancelled={trip.IsCancelled}";

                cancelled.Add((trip, trip.Status));

                trip.Status = TripStatus.Canceled;
                trip.IsCancelled = true;

                string newValue = $"trip.Status={trip.Status}, trip.IsCancelled={trip.IsCancelled}";

                // Creamos el registro de log para cada viaje cancelado
                var tripLog = new TripLog
                {
                    TripId = trip.Id,
                    Status = TripStatus.Canceled,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                };
                _context.TripLogs.Add(tripLog);
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = trip.Id,
                    User = integratorName,
                    Field = "Status",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.UtcNow
                });
            }

            var affected = await _context.SaveChangesAsync();

            foreach (var (cancelledTrip, previousStatus) in cancelled)
            {
                await _tripNotifications.TripCancelledAsync(
                    cancelledTrip,
                    cancelledBy,
                    previousStatus);
            }

            return affected;
        }

        public async Task<List<Trip>> GetIntegrationTripDetailsAsync(DateTime? date, List<string>? externalIds, int? integratorId)
        {
            var query = _context.Trips
                .Include(t => t.Customer)
                .Where(t => t.IntegratorId == integratorId);

            if (date.HasValue)
                query = query.Where(t => t.Date.Date == date.Value.Date);

            if (externalIds != null && externalIds.Any())
                query = query.Where(t => externalIds.Contains(t.TripId));

            return await query.ToListAsync();
        }

        /// <summary>
        /// Stores a batch of trips sent by an integrator, reporting on each one separately.
        /// </summary>
        /// <remarks>
        /// Every trip is stored, or rejected, on its own. A batch used to be one unit of
        /// work with a single save at the end, so one bad row failed the whole request and
        /// the integrator was handed a bare 500 that named neither the trip nor the reason.
        /// </remarks>
        public async Task<IntegrationSyncResultDto> UpsertIntegrationTripsAsync(List<IntegrationTripDto> dtos, int? integratorId, string? integratorName)
        {
            string user = !string.IsNullOrEmpty(integratorName) ? integratorName : "Unknown Integrator";
            user = $"Integrator - {user}";

            var result = new IntegrationSyncResultDto { Timestamp = _clock.UtcNow };

            foreach (var dto in dtos)
            {
                var correlationId = Guid.NewGuid().ToString("N");

                try
                {
                    var outcome = await UpsertSingleIntegrationTripAsync(dto, integratorId, user);

                    result.Results.Add(new IntegrationSyncItemResultDto
                    {
                        TripId = dto.TripId,
                        Status = outcome
                    });
                    result.ProcessedCount++;
                }
                catch (Exception ex)
                {
                    // The failed trip left entities in the tracker belonging to a transaction
                    // that is already rolled back. They go now, or the next trip's save
                    // replays them and fails for a reason that has nothing to do with it.
                    DiscardPendingChanges();

                    var error = IntegrationErrorTranslator.Translate(ex);

                    // A refusal is the endpoint working: the trip was wrong, not the system.
                    // Anything else is ours, and that is where the correlation id earns its
                    // keep, because the cause stays here in full and only a key to it
                    // crosses the wire.
                    if (ex is IntegrationRejectedException)
                    {
                        _logger.LogWarning(
                            "Integration sync refused a trip. CorrelationId={CorrelationId} IntegratorId={IntegratorId} ExternalTripId={ExternalTripId} ErrorCode={ErrorCode}",
                            correlationId,
                            integratorId,
                            dto.TripId,
                            error.Code);
                    }
                    else
                    {
                        _logger.LogError(
                            "Integration sync failed on a trip. CorrelationId={CorrelationId} IntegratorId={IntegratorId} ExternalTripId={ExternalTripId} ErrorCode={ErrorCode} Cause={Cause} Stack={Stack}",
                            correlationId,
                            integratorId,
                            dto.TripId,
                            error.Code,
                            IntegrationErrorTranslator.DescribeForLog(ex),
                            ex.StackTrace);
                    }

                    result.Results.Add(new IntegrationSyncItemResultDto
                    {
                        TripId = dto.TripId,
                        Status = IntegrationSyncStatus.Failed,
                        ErrorCode = error.Code,
                        Message = error.Message,
                        Retryable = error.Retryable,
                        CorrelationId = correlationId
                    });
                    result.FailedCount++;
                }
            }

            result.Success = result.FailedCount == 0;
            result.Message = result.Success
                ? "Synchronization completed successfully."
                : $"{result.ProcessedCount} trips were synchronized and {result.FailedCount} were rejected. See Results for the reason on each.";

            return result;
        }

        /// <summary>
        /// Stores one integration trip, or leaves the database exactly as it found it.
        /// </summary>
        /// <remarks>
        /// The transaction is what makes per-trip reporting honest. The space type, the
        /// funding source and the patient each have to be saved before the trip can point
        /// at them, so without it a trip that fails at the last step would still leave a
        /// patient behind: the integrator would be told the trip was rejected while a
        /// record of that patient quietly existed.
        /// </remarks>
        /// <returns><see cref="IntegrationSyncStatus.Created"/> or <see cref="IntegrationSyncStatus.Updated"/>.</returns>
        private async Task<string> UpsertSingleIntegrationTripAsync(IntegrationTripDto dto, int? integratorId, string user)
        {
            var externalTripId = dto.TripId?.Trim();
            if (string.IsNullOrEmpty(externalTripId))
            {
                throw Reject(
                    IntegrationErrorCode.InvalidTripId,
                    "TripId is blank. It is the identifier this trip is matched on, so without it an update cannot be told apart from a new booking.");
            }

            // The unique index over active trips keys on Date, so a trip carrying a time of
            // day would sit next to the same journey booked at midnight instead of clashing
            // with it. The date of a trip is a calendar day; the window is FromTime/ToTime.
            var tripDate = dto.Date.Date;

            var now = _clock.UtcNow;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. Resolve SpaceType (Unique by Name)
            var spaceTypeName = dto.SpaceTypeName.Trim();
            var spaceType = await _context.SpaceTypes.FirstOrDefaultAsync(s => s.Name == spaceTypeName);
            if (spaceType == null)
            {
                spaceType = new SpaceType
                {
                    Name = spaceTypeName,
                    Description = "Auto-created via Integration",
                    LoadTime = 0,
                    UnloadTime = 0,
                    CapacityTypeId = 1,
                    IsActive = true
                };
                _context.SpaceTypes.Add(spaceType);
                await _context.SaveChangesAsync();
            }

            // 2. Resolve FundingSource (by Name). Trimmed, because nothing stops a second
            // "Medicaid " being created next to the first one: unlike SpaceType, this table
            // has no unique index to catch it.
            var fundingSourceName = string.IsNullOrWhiteSpace(dto.FundingSourceName)
                ? "Unknown"
                : dto.FundingSourceName.Trim();

            var fundingSource = await _context.FundingSources.FirstOrDefaultAsync(f => f.Name == fundingSourceName);
            if (fundingSource == null)
            {
                fundingSource = new FundingSource
                {
                    Name = fundingSourceName,
                    IsActive = true
                };
                _context.FundingSources.Add(fundingSource);
                await _context.SaveChangesAsync();
            }

            // 3. Resolve Customer (Unique by RiderId or Logic: Name + Phone)
            var customer = await ResolveIntegrationCustomerAsync(dto, integratorId, spaceType.Id, fundingSource.Id, now);

            // 4. Resolve Trip (Unique by TripId and by integrator)
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == externalTripId && t.IntegratorId == integratorId);
            bool isNew = trip == null;

            // A cancelled trip is outside the unique index of active trips. Letting a sync
            // quietly clear the flag would put a trip nobody is expecting back on a route,
            // and it is how a cancelled journey ends up colliding with the one booked to
            // replace it.
            if (trip is { IsCancelled: true })
            {
                throw Reject(
                    IntegrationErrorCode.TripCancelled,
                    "This trip was cancelled, and synchronizing it will not reinstate it. If the journey is going ahead again, send it under a new TripId.");
            }

            await GuardAgainstDuplicateActiveTripAsync(dto, tripDate, customer.Id, trip?.Id ?? 0, integratorId);

            if (isNew)
            {
                trip = new Trip
                {
                    TripId = externalTripId,
                    IntegratorId = integratorId,
                    Created = now,
                    Status = TripStatus.Assigned,
                    IsCancelled = false
                };
                _context.Trips.Add(trip);
            }

            // Update Properties
            trip!.Date = tripDate;
            trip.Day = tripDate.DayOfWeek.ToString();
            trip.FromTime = dto.FromTime;
            trip.ToTime = dto.ToTime;
            trip.CustomerId = customer.Id;
            trip.SpaceTypeId = spaceType.Id;
            trip.FundingSourceId = fundingSource.Id;
            trip.PickupAddress = dto.PickupAddress;
            trip.PickupLatitude = dto.PickupLatitude;
            trip.PickupLongitude = dto.PickupLongitude;
            trip.DropoffAddress = dto.DropoffAddress;
            trip.DropoffLatitude = dto.DropoffLatitude;
            trip.DropoffLongitude = dto.DropoffLongitude;
            trip.Distance = dto.Distance;
            trip.PickupCity = dto.PickupCity;
            trip.DropoffCity = dto.DropoffCity;
            trip.Type = dto.Type ?? TripType.Appointment;
            trip.Authorization = dto.Authorization;

            trip.PickupComment = dto.PickupComment;
            trip.DropoffComment = dto.DropoffComment;

            // --- ATTACHMENT (UPSERT / EXPLICIT DELETE) ---

            // We look if the trip already has a file. A brand new trip has no key to
            // look one up by yet, and cannot have an attachment either.
            var existingAttachment = isNew
                ? null
                : await _context.TripAttachments.FirstOrDefaultAsync(a => a.TripId == trip.Id);

            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                using var ms = new MemoryStream();
                await dto.Attachment.CopyToAsync(ms);
                byte[] fileData = ms.ToArray();

                if (existingAttachment == null)
                {
                    _context.TripAttachments.Add(new TripAttachment
                    {
                        Trip = trip,
                        FileName = dto.Attachment.FileName,
                        FileContent = fileData,
                        ContentType = dto.Attachment.ContentType,
                        NotificationEmail = dto.NotificationEmail,
                        Created = now
                    });
                }
                else
                {
                    existingAttachment.FileName = dto.Attachment.FileName;
                    existingAttachment.FileContent = fileData;
                    existingAttachment.ContentType = dto.Attachment.ContentType;
                    existingAttachment.NotificationEmail = dto.NotificationEmail;
                    existingAttachment.Created = now;
                }
            }
            else if (dto.RemoveAttachment && existingAttachment != null)
            {
                // Only on request. Sending no file means "nothing to say about the file",
                // not "throw the paperwork away".
                _context.TripAttachments.Remove(existingAttachment);
            }

            _context.TripHistories.Add(new TripHistory
            {
                // The navigation rather than trip.Id: on a new trip the identity value is
                // still a placeholder here, and pointing at the instance lets EF write the
                // real key once the trip row exists.
                Trip = trip,
                User = user,
                Field = "IntegrationSync",
                PriorValue = isNew ? "N/A" : "Trip Exists",
                NewValue = isNew ? "Trip Created" : "Trip Updated",
                ChangeDate = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return isNew ? IntegrationSyncStatus.Created : IntegrationSyncStatus.Updated;
        }

        /// <summary>
        /// Finds the patient this trip is for, creating the record on first sight.
        /// </summary>
        /// <remarks>
        /// When no RiderId is sent the patient is keyed on name and phone together. Name
        /// alone is not an identity: two patients called John Smith would collapse into one
        /// record and each would start seeing the other's trips. So a trip that carries
        /// neither a RiderId nor a phone number is refused rather than guessed at.
        /// </remarks>
        private async Task<Customer> ResolveIntegrationCustomerAsync(
            IntegrationTripDto dto,
            int? integratorId,
            int spaceTypeId,
            int fundingSourceId,
            DateTime now)
        {
            var riderId = dto.RiderId?.Trim();
            var phone = dto.CustomerPhone?.Trim();
            var fullName = dto.CustomerFullName.Trim();

            if (string.IsNullOrEmpty(riderId) && string.IsNullOrEmpty(phone))
            {
                throw Reject(
                    IntegrationErrorCode.PatientNotIdentifiable,
                    "The patient cannot be identified from what was sent. Provide RiderId, or provide CustomerPhone together with CustomerFullName. "
                    + "With only a name, two different patients who share one would be merged into a single record.");
            }

            var effectiveRiderId = string.IsNullOrEmpty(riderId)
                ? $"{fullName} {phone}".Trim()
                : riderId;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.RiderId == effectiveRiderId);
            if (customer != null)
            {
                return customer;
            }

            customer = new Customer
            {
                RiderId = effectiveRiderId,
                FullName = fullName,
                Phone = phone,
                Address = dto.CustomerAddress ?? "Integration Provided",
                City = dto.CustomerCity ?? "Unknown",
                Zip = dto.CustomerZip ?? "00000",
                State = "N/A",
                Gender = dto.CustomerGender ?? "Unknown",
                FundingSourceId = fundingSourceId,
                SpaceTypeId = spaceTypeId,
                Created = now,
                CreatedBy = "IntegrationSystem",
                IntegratorId = integratorId
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        /// <summary>
        /// Refuses a trip that repeats a journey already booked and still active.
        /// </summary>
        /// <remarks>
        /// The database holds a unique index over the active trips of a patient: same date,
        /// same two addresses, same pickup window. That index is what stops one patient
        /// being sent the same journey twice. This upsert matches on the integrator's own
        /// TripId, which is a different key, so a trip arriving under a new TripId can
        /// describe a journey that already exists and only find out at the insert.
        ///
        /// <para>
        /// Checking it here is what turns a failed insert into an answer. It also decides
        /// how much can be said: the clashing trip is named only when it belongs to the
        /// same integrator. A trip booked by someone else, or entered from the back office,
        /// is another tenant's record, and its identifier is not ours to hand over.
        /// </para>
        /// </remarks>
        private async Task GuardAgainstDuplicateActiveTripAsync(
            IntegrationTripDto dto,
            DateTime tripDate,
            int customerId,
            int currentTripId,
            int? integratorId)
        {
            var clash = await _context.Trips
                .AsNoTracking()
                .Where(t => !t.IsCancelled
                            && t.Id != currentTripId
                            && t.Date == tripDate
                            && t.CustomerId == customerId
                            && t.PickupAddress == dto.PickupAddress
                            && t.DropoffAddress == dto.DropoffAddress
                            && t.FromTime == dto.FromTime
                            && t.ToTime == dto.ToTime)
                .Select(t => new { t.TripId, t.IntegratorId })
                .FirstOrDefaultAsync();

            if (clash == null)
            {
                return;
            }

            const string Preamble =
                "An active trip already exists for this patient on the same date, with the same pickup and dropoff addresses and the same pickup window. ";

            var ours = clash.IntegratorId == integratorId && !string.IsNullOrWhiteSpace(clash.TripId);

            throw Reject(
                IntegrationErrorCode.DuplicateActiveTrip,
                ours
                    ? Preamble + $"You sent it as TripId '{clash.TripId}'. Send an update using that TripId, or cancel it before booking another."
                    : Preamble + "It was not created through this integration, so it cannot be changed here. Contact the provider if the journey needs to change.");
        }

        /// <summary>
        /// Refuses a trip with an answer already worded for the integrator.
        /// </summary>
        private static IntegrationRejectedException Reject(string code, string message, bool retryable = false)
            => new(new IntegrationSyncError(code, message, retryable));

        /// <summary>
        /// Empties the change tracker of work that has been rolled back.
        /// </summary>
        /// <remarks>
        /// Rolling a transaction back does not untrack anything: the entities stay Added
        /// and the next SaveChanges tries to insert them again. Without this, one
        /// malformed trip fails every trip queued behind it in the batch.
        /// </remarks>
        private void DiscardPendingChanges()
        {
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;

                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }

        #region CSV import

        /// <summary>
        /// Space types and patients already resolved in this chunk, by the name they were found under.
        /// </summary>
        /// <remarks>
        /// An entry is written only after the row that resolved it has committed. A row that
        /// fails rolls its transaction back, and a patient created inside it stops existing —
        /// caching that id would leave the next row pointing at a customer that is not there.
        /// </remarks>
        private sealed class ImportBatchCache
        {
            public Dictionary<string, int> SpaceTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, int> Customers { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Stores a chunk of a broker's CSV file, one row at a time, in a single request.
        /// </summary>
        /// <remarks>
        /// The desktop app used to walk the file with five to ten threads, and each row cost up
        /// to six requests of its own: two geocodings, a patient lookup, a patient insert, the
        /// trip, and the history row. Four hundred trips came to roughly two thousand four
        /// hundred requests, which the shared host reads as an attack — it withdraws the
        /// application's permissions, the connection drops, and part of the file never arrives.
        ///
        /// <para>
        /// The work did not get smaller; it moved. Everything a row needs travels with it, so
        /// the lookups happen here, against a context that is already open, and the history row
        /// is written inside the same transaction as the trip.
        /// </para>
        ///
        /// <para>
        /// Per-row transactions are what make the per-row answer honest. The space type and the
        /// patient have to exist before the trip can point at them, so without one a row that
        /// failed at the last step would still leave a patient behind: the office would be told
        /// the trip was rejected while a record of that patient quietly existed.
        /// </para>
        /// </remarks>
        public async Task<TripImportResultDto> ImportTripsAsync(TripImportRequestDto request)
        {
            var user = string.IsNullOrWhiteSpace(_currentUserService.UserName)
                ? "Import"
                : _currentUserService.UserName!;

            var result = new TripImportResultDto { Timestamp = _clock.UtcNow };

            // Read once for the chunk rather than per row: it is the same value for all of them.
            var fundingSourceExists = await _context.FundingSources
                .AsNoTracking()
                .AnyAsync(f => f.Id == request.FundingSourceId);

            if (!fundingSourceExists)
            {
                throw new ArgumentException($"Funding source {request.FundingSourceId} does not exist.");
            }

            var cache = new ImportBatchCache();

            foreach (var item in request.Items)
            {
                var correlationId = Guid.NewGuid().ToString("N");

                try
                {
                    var outcome = await ImportSingleTripAsync(item, request.FundingSourceId, user, cache);

                    result.Results.Add(new TripImportItemResultDto
                    {
                        TripId = item.TripId,
                        Status = outcome
                    });

                    if (outcome == TripImportStatus.Created)
                    {
                        result.CreatedCount++;
                    }
                    else
                    {
                        result.UpdatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    // The failed row left entities in the tracker belonging to a transaction
                    // that is already rolled back. They go now, or the next row's save replays
                    // them and fails for a reason that has nothing to do with it.
                    DiscardPendingChanges();

                    var error = IntegrationErrorTranslator.Translate(ex);

                    // A refusal is the endpoint working: the row was wrong, not the system.
                    // Anything else is ours, and the correlation id is how the cause is handed
                    // over without the message itself crossing the wire — on this schema a
                    // database error quotes the row it rejected, which is patient data.
                    if (ex is IntegrationRejectedException)
                    {
                        _logger.LogWarning(
                            "Trip import refused a row. CorrelationId={CorrelationId} User={User} ExternalTripId={ExternalTripId} ErrorCode={ErrorCode}",
                            correlationId,
                            user,
                            item.TripId,
                            error.Code);
                    }
                    else
                    {
                        _logger.LogError(
                            "Trip import failed on a row. CorrelationId={CorrelationId} User={User} ExternalTripId={ExternalTripId} ErrorCode={ErrorCode} Cause={Cause} Stack={Stack}",
                            correlationId,
                            user,
                            item.TripId,
                            error.Code,
                            IntegrationErrorTranslator.DescribeForLog(ex),
                            ex.StackTrace);
                    }

                    result.Results.Add(new TripImportItemResultDto
                    {
                        TripId = item.TripId,
                        Status = TripImportStatus.Failed,
                        ErrorCode = error.Code,
                        Message = error.Message,
                        Retryable = error.Retryable,
                        CorrelationId = correlationId
                    });
                    result.FailedCount++;
                }
            }

            var stored = result.CreatedCount + result.UpdatedCount;
            result.Success = result.FailedCount == 0;
            result.Message = result.Success
                ? $"{stored} trips imported ({result.CreatedCount} new, {result.UpdatedCount} updated)."
                : $"{stored} trips were imported and {result.FailedCount} were rejected. See Results for the reason on each.";

            return result;
        }

        /// <summary>Stores one row, or leaves the database exactly as it found it.</summary>
        /// <returns><see cref="TripImportStatus.Created"/> or <see cref="TripImportStatus.Updated"/>.</returns>
        private async Task<string> ImportSingleTripAsync(
            TripImportItemDto item,
            int fundingSourceId,
            string user,
            ImportBatchCache cache)
        {
            var externalTripId = item.TripId?.Trim();
            if (string.IsNullOrEmpty(externalTripId))
            {
                throw Reject(
                    IntegrationErrorCode.InvalidTripId,
                    "The row has no TripId. It is the identifier the import is matched on, so without it a trip that already exists cannot be told apart from a new booking.");
            }

            // The unique index over active trips keys on Date, so a trip carrying a time of day
            // would sit next to the same journey booked at midnight instead of clashing with it.
            // The date of a trip is a calendar day; the window is FromTime/ToTime.
            var tripDate = item.Date.Date;
            var now = _clock.UtcNow;

            // Written into the caches only once the transaction below has committed.
            string? stagedSpaceTypeKey = null;
            int stagedSpaceTypeId = 0;
            string? stagedCustomerKey = null;
            int stagedCustomerId = 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. Space type, by its short code.
            var (spaceTypeId, spaceTypeKey, spaceTypeWasCreated) = await ResolveImportSpaceTypeAsync(item, cache);
            if (spaceTypeWasCreated)
            {
                stagedSpaceTypeKey = spaceTypeKey;
                stagedSpaceTypeId = spaceTypeId;
            }

            // 2. Patient.
            var (customerId, customerKey, customerWasCreated) =
                await ResolveImportCustomerAsync(item, fundingSourceId, spaceTypeId, user, now, cache);

            if (customerWasCreated)
            {
                stagedCustomerKey = customerKey;
                stagedCustomerId = customerId;
            }

            // 3. The trip itself, matched on the broker's identifier.
            //
            // Matched across every trip rather than only the office's own, because that is what
            // the desktop app did when it held the whole list in memory. Reading it here instead
            // also fixes a failure mode of that list: it was loaded once when the screen opened,
            // so a trip created afterwards was invisible and the import tried to insert it twice.
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == externalTripId);
            var isNew = trip == null;

            if (isNew)
            {
                trip = new Trip
                {
                    TripId = externalTripId,
                    Created = now,

                    // The office's own bookings land as Assigned. Not read from the row: the
                    // status of a trip is ours to decide, and a re-import must not rewind one
                    // that a driver has already started.
                    Status = TripStatus.Assigned,
                    IsCancelled = false,

                    // Written on creation only — see TripImportItemDto.WillCall.
                    WillCall = item.WillCall
                };

                if (!_currentUserService.IsMilanesInternal && _currentUserService.IntegratorId != null)
                {
                    trip.IntegratorId = _currentUserService.IntegratorId;
                    trip.ProviderId = null;
                }

                _context.Trips.Add(trip);
            }

            // ⚠️ Status, IsCancelled, WillCall, Created and VehicleRouteId are absent below on
            // purpose: they are the fields an update must not touch. A cancelled trip that
            // appears again in tomorrow's file has its details refreshed and stays cancelled —
            // reinstating it would put a vehicle on a route for a journey nobody is expecting.
            trip!.Date = tripDate;
            trip.Day = tripDate.DayOfWeek.ToString();
            trip.FromTime = item.FromTime;
            trip.ToTime = item.ToTime;
            trip.CustomerId = customerId;
            trip.SpaceTypeId = spaceTypeId;
            trip.FundingSourceId = fundingSourceId;
            trip.PickupAddress = item.PickupAddress;
            trip.PickupLatitude = item.PickupLatitude;
            trip.PickupLongitude = item.PickupLongitude;
            trip.DropoffAddress = item.DropoffAddress;
            trip.DropoffLatitude = item.DropoffLatitude;
            trip.DropoffLongitude = item.DropoffLongitude;
            trip.PickupCity = item.PickupCity;
            trip.DropoffCity = item.DropoffCity;
            trip.Distance = item.Distance;
            trip.Type = item.Type ?? TripType.Appointment;
            trip.Pickup = item.Pickup;
            trip.Dropoff = item.Dropoff;
            trip.PickupPhone = item.PickupPhone;
            trip.DropoffPhone = item.DropoffPhone;
            trip.PickupComment = item.PickupComment;
            trip.DropoffComment = item.DropoffComment;

            _context.TripHistories.Add(new TripHistory
            {
                // The navigation rather than trip.Id: on a new trip the identity value is still
                // a placeholder here, and pointing at the instance lets EF write the real key
                // once the trip row exists.
                Trip = trip,
                User = user,
                Field = isNew ? "Add_New_Trip" : "Update_Trip",
                PriorValue = null,
                NewValue = isNew ? "Trip Created (Import)" : "Trip Updated (Import)",
                ChangeDate = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (stagedSpaceTypeKey != null)
            {
                cache.SpaceTypes[stagedSpaceTypeKey] = stagedSpaceTypeId;
            }

            if (stagedCustomerKey != null)
            {
                cache.Customers[stagedCustomerKey] = stagedCustomerId;
            }

            return isNew ? TripImportStatus.Created : TripImportStatus.Updated;
        }

        /// <summary>Finds the space type the row names, creating it on first sight.</summary>
        /// <returns>Its id, the key it is cached under, and whether this call created it.</returns>
        private async Task<(int Id, string Key, bool Created)> ResolveImportSpaceTypeAsync(
            TripImportItemDto item,
            ImportBatchCache cache)
        {
            var name = item.SpaceTypeName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw Reject(
                    IntegrationErrorCode.MissingRequiredField,
                    "The row does not say what kind of space the patient needs. Check the Type column of the file.");
            }

            if (cache.SpaceTypes.TryGetValue(name, out var cached))
            {
                return (cached, name, false);
            }

            var existing = await _context.SpaceTypes
                .AsNoTracking()
                .Where(s => s.Name == name)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (existing != 0)
            {
                cache.SpaceTypes[name] = existing;
                return (existing, name, false);
            }

            var capacityName = string.IsNullOrWhiteSpace(item.CapacityTypeName)
                ? name
                : item.CapacityTypeName.Trim();

            var capacityTypeId = await _context.Capacities
                .AsNoTracking()
                .Where(c => c.Name == capacityName)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (capacityTypeId == 0)
            {
                // The desktop app used to send zero here and let the foreign key reject it, so
                // the office was told a trip had failed without being told what to fix.
                throw Reject(
                    IntegrationErrorCode.InvalidReference,
                    $"The space type '{name}' does not exist and cannot be created, because the capacity '{capacityName}' it would count against is not configured. Create it under Admin before importing this file.");
            }

            var spaceType = new SpaceType
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(item.SpaceTypeDescription) ? name : item.SpaceTypeDescription,
                CapacityTypeId = capacityTypeId,
                LoadTime = 0,
                UnloadTime = 0,
                IsActive = true
            };

            _context.SpaceTypes.Add(spaceType);
            await _context.SaveChangesAsync();

            return (spaceType.Id, name, true);
        }

        /// <summary>
        /// Finds the patient the row is for, creating the record on first sight.
        /// </summary>
        /// <remarks>
        /// Matched on RiderId first and on full name together with phone second, which is the
        /// pair of lookups the desktop app did — one against the list it held in memory, one
        /// against the server. Name alone is never enough: two patients called John Smith would
        /// collapse into one record and each would start seeing the other's trips.
        /// </remarks>
        /// <returns>The patient's id, the key it is cached under, and whether this call created it.</returns>
        private async Task<(int Id, string Key, bool Created)> ResolveImportCustomerAsync(
            TripImportItemDto item,
            int fundingSourceId,
            int spaceTypeId,
            string user,
            DateTime now,
            ImportBatchCache cache)
        {
            var fullName = item.CustomerFullName?.Trim() ?? string.Empty;
            var phone = item.CustomerPhone?.Trim();
            var riderId = item.RiderId?.Trim();

            if (string.IsNullOrEmpty(riderId) && string.IsNullOrEmpty(phone))
            {
                throw Reject(
                    IntegrationErrorCode.PatientNotIdentifiable,
                    "The patient in this row cannot be identified: it carries neither a rider id nor a phone number. With only a name, two different patients who share one would be merged into a single record.");
            }

            var key = string.IsNullOrEmpty(riderId) ? $"{fullName} {phone}".Trim() : riderId;

            if (cache.Customers.TryGetValue(key, out var cached))
            {
                return (cached, key, false);
            }

            var byRiderId = await _context.Customers
                .AsNoTracking()
                .Where(c => c.RiderId == key)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (byRiderId != 0)
            {
                cache.Customers[key] = byRiderId;
                return (byRiderId, key, false);
            }

            if (!string.IsNullOrEmpty(phone))
            {
                var byNameAndPhone = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.FullName == fullName && c.Phone == phone)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                if (byNameAndPhone != 0)
                {
                    cache.Customers[key] = byNameAndPhone;
                    return (byNameAndPhone, key, false);
                }
            }

            var customer = new Customer
            {
                RiderId = key,
                FullName = fullName,
                Phone = phone,
                MobilePhone = item.CustomerMobilePhone,
                Address = item.CustomerAddress ?? string.Empty,
                City = item.CustomerCity ?? string.Empty,
                State = item.CustomerState ?? string.Empty,
                Zip = item.CustomerZip ?? string.Empty,
                Gender = string.IsNullOrWhiteSpace(item.CustomerGender) ? "Male" : item.CustomerGender,
                DOB = item.CustomerDOB,
                FundingSourceId = fundingSourceId,
                SpaceTypeId = spaceTypeId,
                Created = now,
                CreatedBy = user
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return (customer.Id, key, true);
        }

        #endregion

        public async Task UpdateTripTypesAsync(List<TripTypeUpdateDto> updates)
        {
            // Opci�n A: EF Core tradicional (Cargar en memoria y actualizar)
            /*var ids = updates.Select(u => u.Id).ToList();
            var tripsToUpdate = await _context.Trips
                                              .Where(t => ids.Contains(t.Id))
                                              .ToListAsync();

            foreach (var trip in tripsToUpdate)
            {
                var updateData = updates.First(x => x.Id == trip.Id);
                trip.Type = updateData.Type;
            }

            await _context.SaveChangesAsync();*/

            
            // Opci�n B: EF Core 7+ (M�s r�pido, sin cargar entidades)
            foreach (var update in updates)
            {
                await _context.Trips
                    .Where(t => t.Id == update.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Type, update.Type));
            }
            
        }

        /*public async Task<List<Trip>> GetAllAsync2()
        {
            return await _context.Trips
                //.Include(t => t.Customer)
                //.Include(t => t.SpaceType)
                //.Include(t => t.Run)
                .ToListAsync();
        }*/
        public async Task<List<TripReadDto>> GetAllAsync()
        {
            return await _context.Trips
                .AsNoTracking() // Better performance for read-only operations.
                .Include(t => t.Customer) 
                .Include(t => t.SpaceType) 
                .Include(t => t.Run) 
                .Include(t => t.FundingSource) 
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();
        }

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetAllAsync(int pageNumber = 1, int pageSize = 20)
        {
            // Validate parameters
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource);

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.FromTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();

            return (trips, totalCount);
        }

        public async Task<TripReadDto?> GetByIdAsync(int id)
        {
            var t = await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return null;

            return new TripReadDto
            {
                Id = t.Id,
                Day = t.Day,
                Date = t.Date,
                FromTime = t.FromTime,
                ToTime = t.ToTime,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.FullName : null,
                PickupAddress = t.PickupAddress,
                PickupLatitude = t.PickupLatitude,
                PickupLongitude = t.PickupLongitude,
                DropoffAddress = t.DropoffAddress,
                DropoffLatitude = t.DropoffLatitude,
                DropoffLongitude = t.DropoffLongitude,
                SpaceTypeId = t.SpaceTypeId,
                SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                IsCancelled = t.IsCancelled,
                Charge = t.Charge,
                Paid = t.Paid,
                Type = t.Type,
                Pickup = t.Pickup,
                PickupPhone = t.PickupPhone,
                PickupComment = t.PickupComment,
                Dropoff = t.Dropoff,
                DropoffPhone = t.DropoffPhone,
                DropoffComment = t.DropoffComment,
                TripId = t.TripId,
                Authorization = t.Authorization,
                Distance = t.Distance,
                ETA = t.ETA,
                VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                RunName = t.Run != null ? t.Run.Name : null,
                WillCall = t.WillCall,
                Status = t.Status,
                DriverNoShowReason = t.DriverNoShowReason,
                Created = t.Created,
                FundingSourceId = t.FundingSourceId,
                FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
            };
        }

        public async Task<Trip> CreateAsync(TripCreateDto dto)
        {
            // Validate required relationships
            /*var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
            {
                throw new ArgumentException("Invalid Customer ID");
            }

            var spaceTypeExists = await _context.SpaceTypes.AnyAsync(st => st.Id == dto.SpaceTypeId);
            if (!spaceTypeExists)
            {
                throw new ArgumentException("Invalid SpaceType ID");
            }*/

            // Map DTO to Entity
            var trip = new Trip
            {
                Day = dto.Day,
                Date = dto.Date,
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                CustomerId = dto.CustomerId,
                PickupAddress = dto.PickupAddress,
                PickupLatitude = dto.PickupLatitude,
                PickupLongitude = dto.PickupLongitude,
                DropoffAddress = dto.DropoffAddress,
                DropoffLatitude = dto.DropoffLatitude,
                DropoffLongitude = dto.DropoffLongitude,
                SpaceTypeId = dto.SpaceTypeId,
                Type = dto.Type,
                Pickup = dto.Pickup,
                PickupPhone = dto.PickupPhone,
                PickupComment = dto.PickupComment,
                Dropoff = dto.Dropoff,
                DropoffPhone = dto.DropoffPhone,
                DropoffComment = dto.DropoffComment,
                TripId = dto.TripId,
                Authorization = dto.Authorization,
                Distance = dto.Distance,
                ETA = dto.ETA,
                WillCall = dto.WillCall,
                VehicleRouteId = dto.VehicleRouteId,
                DriverNoShowReason = dto.DriverNoShowReason,
                FundingSourceId = dto.FundingSourceId,

                // Optimization
                PickupCity = dto.PickupCity,    
                DropoffCity = dto.DropoffCity,

                // System-managed properties
                Status = TripStatus.Assigned,
                Created = DateTime.UtcNow,
                IsCancelled = false
            };

            // Si el usuario es de un Integrador (Booking), forzamos su ID
            if (!_currentUserService.IsMilanesInternal && _currentUserService.IntegratorId != null)
            {
                trip.IntegratorId = _currentUserService.IntegratorId;
                trip.ProviderId = null; // Por defecto lo har� Milanes
            }

            try
            {
                // Add to context
                _context.Trips.Add(trip);

                // Save changes
                await _context.SaveChangesAsync();

                // L�GICA NUEVA: Si el TripId es nulo o vac�o (viaje manual), 
                // le asignamos el Id autogenerado.
                if (string.IsNullOrWhiteSpace(trip.TripId))
                {
                    trip.TripId = trip.Id.ToString();

                    // Actualizamos solo el campo TripId
                    await _context.SaveChangesAsync();
                }

                return trip;
            }
            catch (DbUpdateException ex)
            {
                // Check if the cause is a unique index violation in SQL Server
                // Error code 2601 or 2627 indicates UNIQUE constraint violation
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                   (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    throw new InvalidOperationException("A similar active trip already exists for this customer on the same date and time.");
                }
                throw; // If it's another error, we'll relaunch it
            }
        }
        
        public async Task<bool> UpdateAsync(int id, TripUpdateDto dto)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;

            trip.Day = dto.Day;
            trip.Date = dto.Date;          
            trip.FromTime = dto.FromTime;
            trip.ToTime = dto.ToTime;
            trip.CustomerId = dto.CustomerId;
            trip.PickupAddress = dto.PickupAddress;
            trip.PickupLatitude = dto.PickupLatitude;
            trip.PickupLongitude = dto.PickupLongitude;
            trip.DropoffAddress = dto.DropoffAddress;
            trip.DropoffLatitude = dto.DropoffLatitude;
            trip.DropoffLongitude = dto.DropoffLongitude;
            trip.SpaceTypeId = dto.SpaceTypeId;

            trip.Type = dto.Type;
            trip.Pickup = dto.Pickup;
            trip.PickupPhone = dto.PickupPhone;
            trip.PickupComment = dto.PickupComment;
            trip.Dropoff = dto.Dropoff;
            trip.DropoffPhone = dto.DropoffPhone;
            trip.DropoffComment = dto.DropoffComment;
            trip.TripId = dto.TripId;
            trip.Authorization = dto.Authorization;
            trip.Distance = dto.Distance;
            trip.ETA = dto.ETA;

            // ⚠️ WillCall is deliberately not written here. It governs a promise to a
            // patient — an hour to get a vehicle there — and the only two doors to it are
            // ActivateWillCallAsync and RevertToWillCallAsync, which record who did it and
            // tell the patient. An edit form that could flip it silently is how a trip
            // stopped being a Will Call with nobody the wiser.
            //trip.VehicleRouteId = (dto.VehicleRouteId == 0) ? null : dto.VehicleRouteId;
            trip.DriverNoShowReason = dto.DriverNoShowReason;
            trip.FundingSourceId = dto.FundingSourceId;

            // Optimization
            trip.PickupCity = dto.PickupCity;
            trip.DropoffCity = dto.DropoffCity;

            // System-managed properties
            //trip.Status = TripStatus.Assigned;
            //trip.Created = DateTime.UtcNow;

            //trip.PickupNote = dto.PickupNote;
            //trip.IsCancelled = dto.IsCancelled; // lo comente porque estaba dando problemas, cuando se importaba por segunda vez el mismo viaje, al actualizar el viaje, siempre se mandaba 0, y si el viaje ya habia sido cancelado se producia esa cotradiccion isCancelled = false y status = cancelled, lo cual no es correcto
         
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                if (IsUniqueConstraintViolation(ex))
                {
                    throw new InvalidOperationException("Another active trip already exists with these same details (Date, Customer, Addresses and Times).");
                }
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TripReadDto>> GetByDateAsync(DateTime date)
        {
            return await _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date == date.Date) // We filter by date (ignoring the time)
                .OrderBy(t => t.FromTime)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0,
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null,
                    PickupCity = t.PickupCity,
                    DropoffCity = t.DropoffCity,
                })
                .ToListAsync();
        }

        public async Task<List<TripReadDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // Normalize dates (ignore time part)
            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;

            // Validate that the range is valid
            if (normalizedStartDate > normalizedEndDate)
            {
                throw new ArgumentException("The start date cannot be greater than the end date");
            }

            return await _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date >= normalizedStartDate && t.Date.Date <= normalizedEndDate)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.FromTime)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0,
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();
        }

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetByDatePaginatedAsync(DateTime date, int pageNumber = 1, int pageSize = 20)
        {
            // Validar par�metros
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var normalizedDate = date.Date;

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date == normalizedDate);

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderBy(t => t.FromTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0,
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();

            return (trips, totalCount);
        }

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetByDateRangePaginatedAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;

            if (normalizedStartDate > normalizedEndDate)
            {
                throw new ArgumentException("The start date cannot be greater than the end date");
            }

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date >= normalizedStartDate && t.Date.Date <= normalizedEndDate);

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.FromTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0,
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();

            return (trips, totalCount);
        }

        public async Task<bool> CancelAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Trip not found
            }

            // You cannot cancel a trip that has already been completed or cancelled.
            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                
                return false;
            }

            var statusBeforeCancellation = trip.Status;

            trip.Status = TripStatus.Canceled;
            trip.IsCancelled = true;

            // Create the status change log.
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Canceled,
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay
            };
            _context.TripLogs.Add(tripLog);

            await _context.SaveChangesAsync();

            // Published after the save: the dispatch office must not be told to refresh
            // a screen over a change that could still be rolled back.
            await _tripNotifications.TripCancelledAsync(
                trip,
                CancelledByTypes.Dispatcher,
                statusBeforeCancellation);

            return true;
        }

        // Ahora se cancelan las 2 patas: A y B, es decir, el viaje principal y los relacionados del mismo cliente para el mismo d�a.
        public async Task<bool> CancelByDriverAsync(int id, string reason, string driverName)
        {
            // 1. Buscamos el viaje principal
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false;
            }

            // Si el viaje ya est� finalizado o cancelado, no hacemos nada.
            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                return false;
            }

            // 2. Buscamos todos los viajes pendientes del mismo cliente para el mismo d�a
            // Filtramos para que no sean el mismo viaje (t.Id != id) 
            // y que no est�n ya finalizados o cancelados.
            var relatedTrips = await _context.Trips
                .Where(t => t.CustomerId == trip.CustomerId &&
                            t.Date.Date == trip.Date.Date && // Comparamos solo la parte de la fecha
                            t.Id != id &&
                            t.Status != TripStatus.Finished  &&
                            t.Status != TripStatus.InProgress &&
                            t.Status != TripStatus.Canceled)
                .ToListAsync();

            // 3. Creamos una lista con todos los viajes a cancelar (el principal + los relacionados)
            var tripsToCancel = new List<Trip> { trip };
            tripsToCancel.AddRange(relatedTrips);

            string user = !string.IsNullOrEmpty(driverName) ? driverName : "Unknown Driver";
            user = $"Driver - {user}";

            // Status before the cancellation, kept per trip: it decides whether the
            // assigned driver is pushed, and by the time we publish it is already gone.
            var cancelled = new List<(Trip Trip, string PreviousStatus)>();

            // 4. Procesamos la cancelaci�n para cada uno
            foreach (var t in tripsToCancel)
            {
                if (t.Status == TripStatus.Finished || t.Status == TripStatus.Canceled)
                {
                    continue;
                }

                cancelled.Add((t, t.Status));

                string priorValue = $"trip.Status={t.Status}, trip.IsCancelled={t.IsCancelled}";

                t.Status = TripStatus.Canceled;
                t.IsCancelled = true;
                t.DriverNoShowReason = reason;

                string newValue = $"trip.Status={t.Status}, trip.IsCancelled={t.IsCancelled}, trip.DriverNoShowReason={reason}";
                
                // Creamos el registro de log para cada viaje cancelado
                var tripLog = new TripLog
                {
                    TripId = t.Id,
                    Status = TripStatus.Canceled,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    // Opcional: se puede poner en las notas que fue cancelado en cascada
                    // Notes = t.Id == id ? $"Directly cancelled: {reason}" : $"Auto-cancelled due to main trip {id} cancellation."
                };
                _context.TripLogs.Add(tripLog);
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = t.Id,
                    User = user,
                    Field = "Status",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.UtcNow
                });
            }

            // 5. Guardamos todos los cambios en una sola transacci�n
            await _context.SaveChangesAsync();

            // One event per cancelled trip: the driver cancelling the outbound leg also
            // drops the return, and the patient has to be told about both.
            foreach (var (cancelledTrip, previousStatus) in cancelled)
            {
                await _tripNotifications.TripCancelledAsync(
                    cancelledTrip,
                    CancelledByTypes.Driver,
                    previousStatus,
                    reason);
            }

            return true;
        }

        // Solo la pata A
        public async Task<bool> CancelByDriverAsyncOld(int id, string reason)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; 
            }

            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                return false; // You cannot cancel a trip that has already been completed or cancelled.
            }

            // We assign the new status and reason
            trip.Status = TripStatus.Canceled;
            trip.IsCancelled = true;
            trip.DriverNoShowReason = reason; // <-- We save the driver's motive

            // We create the state change record
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Canceled,
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay,
                //Notes = $"Cancelled by driver. Reason: {reason}" 
            };
            _context.TripLogs.Add(tripLog);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// The office activates a Will Call on the patient's behalf: they rang the office
        /// instead of pressing the button in their app.
        /// </summary>
        /// <param name="fromTime">
        /// Pickup time the dispatcher settled on. Null means now, and "now" is wall-clock
        /// time where the trip is operated — never the hour of the machine running this.
        /// </param>
        /// <remarks>
        /// ⚠️ One of the two doors to <c>Trip.WillCall</c>. From this instant the office has
        /// an hour to get a vehicle to the patient, and the hour is counted from here, not
        /// from the pickup time chosen: a dispatcher writing a later hour does not buy the
        /// office more time.
        /// </remarks>
        public async Task<bool> ActivateWillCallAsync(int id, TimeSpan? fromTime)
        {
            var trip = await _context.Trips.FindAsync(id);

            if (trip is null || trip.IsCancelled || !trip.WillCall)
                return false;

            var activatedAtUtc = _clock.UtcNow;

            var priorValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

            trip.FromTime = fromTime ?? _clock.TimeOfDayFor(trip.ProviderId);
            trip.Status = TripStatus.Waiting;
            trip.WillCall = false;

            WriteWillCallHistory(trip, priorValue);

            await _context.SaveChangesAsync();

            // Outside any catch, and after the save: a notification that could not be sent
            // must never make anybody believe the activation was not registered.
            //
            // The patient is told this time, unlike when they press the button themselves:
            // somebody did it on their behalf and they have not seen any confirmation.
            await _tripNotifications.WillCallActivatedAsync(
                trip,
                activatedAtUtc,
                notifyRider: true);

            return true;
        }

        /// <summary>
        /// The trip goes back to waiting for the patient to say they are ready.
        /// </summary>
        /// <param name="fromTime">
        /// Pickup time to leave on the trip. Null means the 23:59 the office reads as
        /// "waiting on the patient".
        /// </param>
        /// <remarks>
        /// ⚠️ The other door to <c>Trip.WillCall</c>. Undoing a mistaken activation and
        /// turning an ordinary trip into a Will Call are the same operation, which is why
        /// this is not called an "undo": both end with a trip waiting on its patient.
        /// </remarks>
        public async Task<bool> RevertToWillCallAsync(int id, TimeSpan? fromTime)
        {
            var trip = await _context.Trips.FindAsync(id);

            if (trip is null || trip.IsCancelled || trip.WillCall)
                return false;

            var priorValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

            trip.FromTime = fromTime ?? WillCallPickupTime;
            trip.Status = TripStatus.Assigned;
            trip.WillCall = true;

            WriteWillCallHistory(trip, priorValue);

            await _context.SaveChangesAsync();

            await _tripNotifications.WillCallCreatedAsync(trip);

            return true;
        }

        /// <summary>
        /// Records who moved the Will Call flag, and what it looked like before.
        /// </summary>
        /// <remarks>
        /// The whole point of putting this field behind two operations: whatever happens to
        /// it now has a name and a time against it.
        /// </remarks>
        private void WriteWillCallHistory(Trip trip, string priorValue)
        {
            var newValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

            _context.TripLogs.Add(new TripLog
            {
                TripId = trip.Id,
                Status = trip.Status,
                Date = _clock.UtcNow.Date,
                Time = _clock.UtcNow.TimeOfDay
            });

            _context.TripHistories.Add(new TripHistory
            {
                TripId = trip.Id,
                User = ResolveActorName(),
                Field = "WillCall",
                PriorValue = priorValue,
                NewValue = newValue,
                ChangeDate = _clock.UtcNow
            });
        }

        private string ResolveActorName()
        {
            var name = _currentUserService?.UserName;

            return string.IsNullOrWhiteSpace(name)
                ? "Dispatcher"
                : $"Dispatcher - {name}";
        }

        public async Task<bool> UncancelAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Trip not found
            }

            // You can only reverse a canceled trip.
            if (trip.Status != TripStatus.Canceled)
            {
                return false; // The ride is not in the correct state for this operation
            }

            // Revert the status to 'Assigned' or the default status
            trip.Status = TripStatus.Assigned;
            trip.IsCancelled = false;

            // Record the change
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Assigned, 
                //Comment = "Trip cancellation has been reversed.", 
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay
            };
            _context.TripLogs.Add(tripLog);
           
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (IsUniqueConstraintViolation(ex))
                {
                    throw new InvalidOperationException("Cannot uncancel this trip because there is already another active trip with the same details for this customer.");
                }
                throw;
            }

            // The patient was told their ride was gone. Telling them it is back is the
            // other half of that conversation, and until now nobody said anything at all.
            // Published after the save and outside the catch: the trip is reactivated
            // whether or not anybody managed to be told.
            await _tripNotifications.TripReactivatedAsync(trip);

            return true;
        }

        public async Task<bool> UpdateFromDispatchAsync(int id, TripDispatchUpdateDto dto)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Not found
            }

            // Map only allowed fields from this DTO.
            //
            // ⚠️ WillCall is not one of them, however much the DTO still carries it. See
            // ActivateWillCallAsync and RevertToWillCallAsync: those are the only two
            // writers, and they leave a history row and a notification behind.
            trip.Type = dto.Type;
            trip.FromTime = dto.FromTime;
            trip.PickupPhone = dto.PickupPhone;
            trip.PickupComment = dto.PickupComment;
            trip.DropoffPhone = dto.DropoffPhone;
            trip.DropoffComment = dto.DropoffComment;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRunAsync(int id, int? vehicleRouteId)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Not found
            }           

            trip.VehicleRouteId = vehicleRouteId;
            // Si se asigna a null, el estado vuelve a ser "Accepted" 
            trip.Status = vehicleRouteId.HasValue ? TripStatus.Scheduled : TripStatus.Accepted;

            await _context.SaveChangesAsync();
          
            return true;
        }

        public async Task<bool> StartTripAsync(int id, TimeSpan? travel)
        {
            var trip = await _context.Trips
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
                return false;

            if (trip.Status == TripStatus.Started)
                throw new InvalidOperationException(
                    "The trip has already been started.");

            if (trip.Status != TripStatus.Scheduled)
                throw new InvalidOperationException(
                    "The trip cannot be started because it is not scheduled.");

            trip.Status = TripStatus.Started;

            await _context.SaveChangesAsync();

            await _tripNotifications.DriverStartedTripAsync(trip, travel);

            return true;
        }
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                   (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }

    }// end class

}


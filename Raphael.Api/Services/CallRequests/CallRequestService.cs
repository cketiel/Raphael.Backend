using System.Buffers.Binary;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Raphael.Api.Realtime;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.CallRequests;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs.CallRequests;
using Raphael.Shared.DTOs.Realtime;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.CallRequests;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.CallRequests
{
    /// <inheritdoc cref="ICallRequestService"/>
    /// <remarks>
    /// Every change is saved first and announced after. The board message and the driver's push
    /// describe something that already happened, and neither can undo it by failing.
    /// </remarks>
    public class CallRequestService : ICallRequestService
    {
        private const int NameMaxLength = 150;

        private readonly RaphaelContext _context;
        private readonly IOperationClock _clock;
        private readonly IDispatchBroadcaster _board;
        private readonly NotificationService _notificationService;
        private readonly ILogger<CallRequestService> _logger;
        private readonly TimeSpan _signalGap;

        public CallRequestService(
            RaphaelContext context,
            IOperationClock clock,
            IDispatchBroadcaster board,
            NotificationService notificationService,
            IOptions<CallRequestOptions> options,
            ILogger<CallRequestService> logger)
        {
            _context = context;
            _clock = clock;
            _board = board;
            _notificationService = notificationService;
            _logger = logger;
            _signalGap = TimeSpan.FromSeconds(Math.Max(0, options.Value.MinSecondsBetweenDriverSignals));
        }

        #region Driver

        public async Task<DriverCallRequestDto> GetDriverStateAsync(
            int driverId,
            CancellationToken cancellationToken = default)
        {
            var now = _clock.UtcNow;
            var driver = await LoadDriverAsync(driverId, cancellationToken);
            var today = _clock.TodayFor(driver?.ProviderId);

            var open = await OpenRequestOf(driverId)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            // A case left open from an earlier day is the office's to close; the driver starts fresh.
            if (open is not null && open.OperatingDate < today)
                open = null;

            var nextAllowed = open is not null
                ? open.LastDriverSignalAtUtc.Add(_signalGap)
                : await CreationThrottleAsync(driverId, cancellationToken);

            return DriverState(open, driver, nextAllowed, accepted: false, now);
        }

        public async Task<DriverCallRequestDto> RequestOrRemindAsync(
            int driverId,
            CreateCallRequestDto input,
            CancellationToken cancellationToken = default)
        {
            var driver = await LoadDriverAsync(driverId, cancellationToken);

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return await RequestOrRemindOnceAsync(driverId, driver, input, cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (attempt == 0)
                {
                    // The office changed the case at the same moment. Look again, once.
                    _context.ChangeTracker.Clear();
                }
            }
        }

        private async Task<DriverCallRequestDto> RequestOrRemindOnceAsync(
            int driverId,
            DriverInfo? driver,
            CreateCallRequestDto input,
            CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;
            var today = _clock.TodayFor(driver?.ProviderId);

            var open = await OpenRequestOf(driverId).FirstOrDefaultAsync(cancellationToken);

            if (open is not null && open.OperatingDate < today)
            {
                open.Status = CallRequestStatus.Expired;
                open.ClosedAtUtc = now;
                AddEvent(open, CallRequestEventType.Expired, now, null, null);

                await _context.SaveChangesAsync(cancellationToken);
                await AnnounceAsync(open, CallRequestEventType.Expired, null, cancellationToken);

                open = null;
            }

            if (open is not null)
            {
                var allowedAt = open.LastDriverSignalAtUtc.Add(_signalGap);

                if (now < allowedAt)
                    return DriverState(open, driver, allowedAt, accepted: false, now);

                open.ReminderCount++;
                open.LastReminderAtUtc = now;
                open.LastDriverSignalAtUtc = now;
                AddEvent(open, CallRequestEventType.Reminded, now, driverId, driver?.Name);

                await _context.SaveChangesAsync(cancellationToken);
                await AnnounceAsync(open, CallRequestEventType.Reminded, driverId, cancellationToken);

                return DriverState(open, driver, now.Add(_signalGap), accepted: true, now);
            }

            var throttledUntil = await CreationThrottleAsync(driverId, cancellationToken);

            if (throttledUntil.HasValue && now < throttledUntil.Value)
                return DriverState(null, driver, throttledUntil, accepted: false, now);

            var (routeId, routeName) = await ResolveRouteAsync(driverId, input.VehicleRouteId, today, cancellationToken);
            var scheduleId = await ResolveScheduleAsync(input.ScheduleId, routeId, cancellationToken);
            var (latitude, longitude) = ValidCoordinates(input.Latitude, input.Longitude);

            var request = new DriverCallRequest
            {
                DriverId = driverId,
                DriverName = Truncate(driver?.Name) ?? $"Driver {driverId}",
                VehicleRouteId = routeId,
                RouteName = routeName,
                ProviderId = driver?.ProviderId,
                OperatingDate = today,
                ScheduleId = scheduleId,
                RequestLatitude = latitude,
                RequestLongitude = longitude,
                Status = CallRequestStatus.Waiting,
                RequestedAtUtc = now,
                QueuedAtUtc = now,
                LastDriverSignalAtUtc = now
            };

            AddEvent(request, CallRequestEventType.Requested, now, driverId, driver?.Name);

            _context.DriverCallRequests.Add(request);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Two presses raced and the other one opened the case; this one changes nothing.
                _context.ChangeTracker.Clear();

                var winner = await OpenRequestOf(driverId).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

                return DriverState(winner, driver, winner?.LastDriverSignalAtUtc.Add(_signalGap), accepted: false, now);
            }

            await AnnounceAsync(request, CallRequestEventType.Requested, driverId, cancellationToken);

            return DriverState(request, driver, now.Add(_signalGap), accepted: true, now);
        }

        public async Task<CallRequestResult<DriverCallRequestDto>> DriverAvailableAsync(
            int driverId,
            int requestId,
            CancellationToken cancellationToken = default)
        {
            var driver = await LoadDriverAsync(driverId, cancellationToken);

            for (var attempt = 0; ; attempt++)
            {
                var request = await _context.DriverCallRequests
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.DriverId == driverId, cancellationToken);

                if (request is null)
                    return CallRequestResult<DriverCallRequestDto>.NotFound();

                var now = _clock.UtcNow;

                // Only answers a missed call, and is used up by answering it, so it needs no throttle:
                // holding it back would tell the office late that the driver can talk right now.
                if (!IsMissedCallPending(request))
                    return CallRequestResult<DriverCallRequestDto>.Ok(
                        DriverState(request, driver, request.LastDriverSignalAtUtc.Add(_signalGap), accepted: false, now));

                request.DriverAvailableAtUtc = now;
                request.LastDriverSignalAtUtc = now;
                AddEvent(request, CallRequestEventType.DriverAvailable, now, driverId, driver?.Name);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (attempt == 0)
                {
                    _context.ChangeTracker.Clear();
                    continue;
                }

                await AnnounceAsync(request, CallRequestEventType.DriverAvailable, driverId, cancellationToken);

                return CallRequestResult<DriverCallRequestDto>.Ok(
                    DriverState(request, driver, now.Add(_signalGap), accepted: true, now));
            }
        }

        public async Task<CallRequestResult<DriverCallRequestDto>> CancelByDriverAsync(
            int driverId,
            int requestId,
            CancellationToken cancellationToken = default)
        {
            var driver = await LoadDriverAsync(driverId, cancellationToken);

            for (var attempt = 0; ; attempt++)
            {
                var request = await _context.DriverCallRequests
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.DriverId == driverId, cancellationToken);

                if (request is null)
                    return CallRequestResult<DriverCallRequestDto>.NotFound();

                var now = _clock.UtcNow;

                if (!request.IsOpen)
                    return CallRequestResult<DriverCallRequestDto>.Ok(
                        DriverState(null, driver, await CreationThrottleAsync(driverId, cancellationToken), accepted: false, now));

                request.Status = CallRequestStatus.Cancelled;
                request.ClosedAtUtc = now;
                request.LastDriverSignalAtUtc = now;
                AddEvent(request, CallRequestEventType.Cancelled, now, driverId, driver?.Name);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (attempt == 0)
                {
                    _context.ChangeTracker.Clear();
                    continue;
                }

                await AnnounceAsync(request, CallRequestEventType.Cancelled, driverId, cancellationToken);

                return CallRequestResult<DriverCallRequestDto>.Ok(
                    DriverState(null, driver, now.Add(_signalGap), accepted: true, now));
            }
        }

        /// <summary>
        /// When the driver may open a new case, if they may not yet. Only after their own
        /// cancellation: create, cancel, create again is the loop the gap is there for. A case the
        /// office closed never makes the driver wait.
        /// </summary>
        private async Task<DateTime?> CreationThrottleAsync(int driverId, CancellationToken cancellationToken)
        {
            var last = await _context.DriverCallRequests
                .AsNoTracking()
                .Where(x => x.DriverId == driverId)
                .OrderByDescending(x => x.Id)
                .Select(x => new { x.Status, x.LastDriverSignalAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            return last is { Status: CallRequestStatus.Cancelled }
                ? last.LastDriverSignalAtUtc.Add(_signalGap)
                : null;
        }

        /// <summary>The route the driver named, if it is theirs; otherwise today's, if they have only one.</summary>
        private async Task<(int? Id, string? Name)> ResolveRouteAsync(
            int driverId,
            int? requestedRouteId,
            DateTime today,
            CancellationToken cancellationToken)
        {
            if (requestedRouteId is > 0)
            {
                var route = await _context.VehicleRoutes
                    .AsNoTracking()
                    .Where(r => r.Id == requestedRouteId.Value && r.DriverId == driverId)
                    .Select(r => new { r.Id, r.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (route is not null)
                    return (route.Id, route.Name);
            }

            var tomorrow = today.AddDays(1);

            var candidates = await _context.Schedules
                .AsNoTracking()
                .Where(s => s.VehicleRoute.DriverId == driverId && s.Date >= today && s.Date < tomorrow)
                .Select(s => new { s.VehicleRouteId, s.VehicleRoute.Name })
                .Distinct()
                .Take(2)
                .ToListAsync(cancellationToken);

            return candidates.Count == 1
                ? (candidates[0].VehicleRouteId, candidates[0].Name)
                : (null, null);
        }

        private async Task<int?> ResolveScheduleAsync(int? scheduleId, int? routeId, CancellationToken cancellationToken)
        {
            if (scheduleId is not > 0 || routeId is null)
                return null;

            var belongs = await _context.Schedules
                .AsNoTracking()
                .AnyAsync(s => s.Id == scheduleId.Value && s.VehicleRouteId == routeId.Value, cancellationToken);

            return belongs ? scheduleId : null;
        }

        private static (double? Latitude, double? Longitude) ValidCoordinates(double? latitude, double? longitude)
        {
            var valid = latitude is >= -90 and <= 90
                        && longitude is >= -180 and <= 180
                        && !(latitude == 0 && longitude == 0);

            return valid ? (latitude, longitude) : (null, null);
        }

        private DriverCallRequestDto DriverState(
            DriverCallRequest? request,
            DriverInfo? driver,
            DateTime? nextAllowed,
            bool accepted,
            DateTime now)
        {
            var state = new DriverCallRequestDto
            {
                SignalAccepted = accepted,
                CallbackPhone = driver?.PhoneNumber,
                ServerTimeUtc = Utc(now),
                NextSignalAllowedAtUtc = nextAllowed.HasValue && nextAllowed.Value > now ? Utc(nextAllowed.Value) : null
            };

            if (request is not { IsOpen: true })
                return state;

            state.HasOpenRequest = true;
            state.Id = request.Id;
            state.Status = request.Status.ToString();
            state.RequestedAtUtc = Utc(request.RequestedAtUtc);
            state.ReminderCount = request.ReminderCount;
            state.LastReminderAtUtc = Utc(request.LastReminderAtUtc);
            state.ClaimedByFirstName = FirstName(request.ClaimedByName);
            state.ClaimedAtUtc = Utc(request.ClaimedAtUtc);
            state.CallAttempts = request.CallAttempts;
            state.LastAttemptAtUtc = Utc(request.LastAttemptAtUtc);
            state.MissedCallPending = IsMissedCallPending(request);

            return state;
        }

        private static bool IsMissedCallPending(DriverCallRequest request) =>
            request.Status == CallRequestStatus.InProgress
            && request.LastAttemptAtUtc.HasValue
            && (request.DriverAvailableAtUtc is null || request.DriverAvailableAtUtc < request.LastAttemptAtUtc);

        private sealed record DriverInfo(string Name, string? PhoneNumber, int? ProviderId);

        private Task<DriverInfo?> LoadDriverAsync(int driverId, CancellationToken cancellationToken) =>
            _context.Users
                .AsNoTracking()
                .Where(u => u.Id == driverId)
                .Select(u => new DriverInfo(u.FullName, u.PhoneNumber, u.ProviderId))
                .FirstOrDefaultAsync(cancellationToken);

        private IQueryable<DriverCallRequest> OpenRequestOf(int driverId) =>
            _context.DriverCallRequests.Where(x =>
                x.DriverId == driverId &&
                (x.Status == CallRequestStatus.Waiting || x.Status == CallRequestStatus.InProgress));

        #endregion

        #region Office

        public async Task<OfficeCaller> ResolveOfficeCallerAsync(
            int userId,
            int? providerId,
            CancellationToken cancellationToken = default)
        {
            var name = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            return new OfficeCaller(userId, Truncate(name) ?? $"User {userId}", providerId);
        }

        public async Task<IReadOnlyList<CallRequestSummaryDto>> GetQueueAsync(
            OfficeCaller caller,
            CancellationToken cancellationToken = default)
        {
            var today = _clock.TodayFor(caller.ProviderId);

            var rows = await Scoped(caller)
                .AsNoTracking()
                .Where(x =>
                    x.Status == CallRequestStatus.Waiting ||
                    x.Status == CallRequestStatus.InProgress ||
                    x.OperatingDate == today)
                .ToListAsync(cancellationToken);

            var numbers = await NumbersOfDayAsync(rows, cancellationToken);

            return rows
                .OrderBy(x => x.IsOpen ? 0 : 1)
                .ThenBy(x => x.QueuedAtUtc)
                .Select(x => ToSummary(x, numbers.GetValueOrDefault(x.Id, 1)))
                .ToList();
        }

        public async Task<CallRequestDetailDto?> GetDetailAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default)
        {
            var request = await Scoped(caller)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (request is null)
                return null;

            var sameDay = await Scoped(caller)
                .AsNoTracking()
                .Where(x => x.DriverId == request.DriverId && x.OperatingDate == request.OperatingDate)
                .OrderBy(x => x.RequestedAtUtc)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var numbers = sameDay
                .Select((x, index) => (x.Id, Number: index + 1))
                .ToDictionary(x => x.Id, x => x.Number);

            var timeline = await _context.DriverCallRequestEvents
                .AsNoTracking()
                .Where(e => e.CallRequestId == id)
                .OrderBy(e => e.AtUtc)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

            var detail = new CallRequestDetailDto
            {
                Request = ToSummary(request, numbers.GetValueOrDefault(request.Id, 1)),
                DriverPhone = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == request.DriverId)
                    .Select(u => u.PhoneNumber)
                    .FirstOrDefaultAsync(cancellationToken),
                ResolutionNote = request.ResolutionNote,
                Timeline = timeline
                    .Select(e => new CallRequestTimelineItemDto
                    {
                        Type = e.Type.ToString(),
                        AtUtc = Utc(e.AtUtc),
                        ByName = e.ByName,
                        Detail = e.Detail
                    })
                    .ToList(),
                OtherRequestsOfDay = sameDay
                    .Where(x => x.Id != request.Id)
                    .Select(x => ToSummary(x, numbers.GetValueOrDefault(x.Id, 1)))
                    .ToList()
            };

            if (request.RequestLatitude is double latitude && request.RequestLongitude is double longitude)
            {
                detail.RequestPosition = new CallRequestPositionDto
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    AtUtc = Utc(request.RequestedAtUtc)
                };
            }

            if (request.VehicleRouteId is int routeId)
            {
                detail.VehicleName = await _context.VehicleRoutes
                    .AsNoTracking()
                    .Where(r => r.Id == routeId)
                    .Select(r => r.Vehicle.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                detail.Route = await RouteContextAsync(routeId, request.OperatingDate, request.ScheduleId, cancellationToken);

                var fix = await _context.GPSData
                    .AsNoTracking()
                    .Where(g => g.IdVehicleRoute == routeId)
                    .OrderByDescending(g => g.DateTime)
                    .Select(g => new { g.Latitude, g.Longitude, g.DateTime, g.Speed, g.Address })
                    .FirstOrDefaultAsync(cancellationToken);

                if (fix is not null)
                {
                    detail.LastPosition = new CallRequestPositionDto
                    {
                        Latitude = fix.Latitude,
                        Longitude = fix.Longitude,
                        AtUtc = Utc(fix.DateTime),
                        Speed = fix.Speed,
                        Address = fix.Address
                    };
                }
            }

            return detail;
        }

        public Task<CallRequestResult<CallRequestSummaryDto>> ClaimAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default) =>
            ChangeAsync(caller, id, (request, now) => Claim(request, now, caller), cancellationToken);

        public Task<CallRequestResult<CallRequestSummaryDto>> TakeOverAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default) =>
            ChangeAsync(caller, id, (request, now) => TakeOver(request, now, caller), cancellationToken);

        public Task<CallRequestResult<CallRequestSummaryDto>> ReleaseAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default) =>
            ChangeAsync(caller, id, (request, now) => Release(request, now, caller), cancellationToken);

        public Task<CallRequestResult<CallRequestSummaryDto>> NoAnswerAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default) =>
            ChangeAsync(caller, id, (request, now) => NoAnswer(request, now, caller), cancellationToken);

        public async Task<CallRequestResult<CallRequestSummaryDto>> ResolveAsync(
            OfficeCaller caller,
            int id,
            ResolveCallRequestDto input,
            CancellationToken cancellationToken = default)
        {
            var code = input?.ReasonCode?.Trim().ToUpperInvariant();

            if (!CallRequestReasonCodes.IsValid(code))
                return CallRequestResult<CallRequestSummaryDto>.Invalid("Choose why this request is being closed.");

            var note = string.IsNullOrWhiteSpace(input!.Note) ? null : input.Note.Trim();

            if (code == CallRequestReasonCodes.Other && note is null)
                return CallRequestResult<CallRequestSummaryDto>.Invalid("Describe the reason when choosing Other.");

            if (note is { Length: > CallRequestReasonCodes.NoteMaxLength })
                return CallRequestResult<CallRequestSummaryDto>.Invalid(
                    $"The note can have at most {CallRequestReasonCodes.NoteMaxLength} characters.");

            return await ChangeAsync(
                caller,
                id,
                (request, now) => Resolve(request, now, caller, code!, note),
                cancellationToken);
        }

        public Task<CallRequestResult<CallRequestSummaryDto>> ReopenAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default) =>
            ChangeAsync(caller, id, (request, now) => Reopen(request, now, caller), cancellationToken);

        private static Decision Claim(DriverCallRequest request, DateTime now, OfficeCaller caller)
        {
            if (request.Status == CallRequestStatus.InProgress && request.ClaimedByUserId == caller.UserId)
                return Decision.Unchanged;

            if (request.Status == CallRequestStatus.InProgress)
                return Decision.Conflict($"{request.ClaimedByName} is already handling this request.");

            if (request.Status != CallRequestStatus.Waiting)
                return Decision.Conflict("This request is already closed.");

            request.Status = CallRequestStatus.InProgress;
            SetClaim(request, caller, now);
            AddEvent(request, CallRequestEventType.Claimed, now, caller.UserId, caller.Name);

            return Decision.Made(CallRequestEventType.Claimed, notifyDriver: true);
        }

        private static Decision TakeOver(DriverCallRequest request, DateTime now, OfficeCaller caller)
        {
            if (request.Status == CallRequestStatus.Waiting)
                return Claim(request, now, caller);

            if (request.Status != CallRequestStatus.InProgress)
                return Decision.Conflict("This request is already closed.");

            if (request.ClaimedByUserId == caller.UserId)
                return Decision.Unchanged;

            var previous = request.ClaimedByName;

            SetClaim(request, caller, now);
            AddEvent(request, CallRequestEventType.TakenOver, now, caller.UserId, caller.Name, previous);

            // No push: the driver was already told the office is on it.
            return Decision.Made(CallRequestEventType.TakenOver);
        }

        private static Decision Release(DriverCallRequest request, DateTime now, OfficeCaller caller)
        {
            if (request.Status == CallRequestStatus.Waiting)
                return Decision.Unchanged;

            if (request.Status != CallRequestStatus.InProgress)
                return Decision.Conflict("This request is already closed.");

            if (request.ClaimedByUserId != caller.UserId)
                return Decision.Conflict($"{request.ClaimedByName} is handling this request. Take it over first.");

            // QueuedAtUtc stays: the driver has been waiting since then and keeps their place.
            request.Status = CallRequestStatus.Waiting;
            ClearClaim(request);
            AddEvent(request, CallRequestEventType.Released, now, caller.UserId, caller.Name);

            return Decision.Made(CallRequestEventType.Released);
        }

        private static Decision NoAnswer(DriverCallRequest request, DateTime now, OfficeCaller caller)
        {
            if (request.Status == CallRequestStatus.Waiting)
                return Decision.Conflict("Take this request before logging a call.");

            if (request.Status != CallRequestStatus.InProgress)
                return Decision.Conflict("This request is already closed.");

            if (request.ClaimedByUserId != caller.UserId)
                return Decision.Conflict($"{request.ClaimedByName} is handling this request. Take it over first.");

            request.CallAttempts++;
            request.LastAttemptAtUtc = now;
            AddEvent(request, CallRequestEventType.CallNotAnswered, now, caller.UserId, caller.Name);

            return Decision.Made(CallRequestEventType.CallNotAnswered);
        }

        private static Decision Resolve(
            DriverCallRequest request,
            DateTime now,
            OfficeCaller caller,
            string reasonCode,
            string? note)
        {
            if (!request.IsOpen)
                return Decision.Conflict("This request is already closed.");

            if (request.Status == CallRequestStatus.InProgress && request.ClaimedByUserId != caller.UserId)
                return Decision.Conflict($"{request.ClaimedByName} is handling this request. Take it over first.");

            // Closed without being taken first: the attention starts and ends now.
            if (request.Status == CallRequestStatus.Waiting)
                SetClaim(request, caller, now);

            request.Status = CallRequestStatus.Resolved;
            request.ResolvedByUserId = caller.UserId;
            request.ResolvedByName = caller.Name;
            request.ClosedAtUtc = now;
            request.ReasonCode = reasonCode;
            request.ResolutionNote = note;
            AddEvent(request, CallRequestEventType.Resolved, now, caller.UserId, caller.Name, reasonCode);

            return Decision.Made(CallRequestEventType.Resolved);
        }

        private Decision Reopen(DriverCallRequest request, DateTime now, OfficeCaller caller)
        {
            if (request.IsOpen)
                return Decision.Unchanged;

            if (request.Status == CallRequestStatus.Expired)
                return Decision.Conflict("An expired request cannot be reopened.");

            if (request.OperatingDate.Date != _clock.TodayFor(request.ProviderId).Date)
                return Decision.Conflict("Only today's requests can be reopened.");

            request.Status = CallRequestStatus.Waiting;
            request.QueuedAtUtc = now;
            ClearClaim(request);
            request.ResolvedByUserId = null;
            request.ResolvedByName = null;
            request.ClosedAtUtc = null;
            request.ReasonCode = null;
            request.ResolutionNote = null;
            AddEvent(request, CallRequestEventType.Reopened, now, caller.UserId, caller.Name);

            return Decision.Made(CallRequestEventType.Reopened);
        }

        /// <summary>
        /// Loads, decides, saves, and only then tells the other screens and the driver.
        /// </summary>
        /// <remarks>
        /// ⚠️ The row version is what stops two dispatchers taking the same case: both read
        /// Waiting, the first save wins, and the second gets a conflict with who won.
        /// </remarks>
        private async Task<CallRequestResult<CallRequestSummaryDto>> ChangeAsync(
            OfficeCaller caller,
            int id,
            Func<DriverCallRequest, DateTime, Decision> decide,
            CancellationToken cancellationToken)
        {
            var request = await Scoped(caller).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (request is null)
                return CallRequestResult<CallRequestSummaryDto>.NotFound();

            var decision = decide(request, _clock.UtcNow);

            if (decision.Outcome == CallRequestOutcome.Conflict)
                return CallRequestResult<CallRequestSummaryDto>.Conflict(
                    await SummaryOfAsync(request, cancellationToken),
                    decision.Message ?? "This request changed.");

            if (decision.Change is null)
                return CallRequestResult<CallRequestSummaryDto>.Ok(await SummaryOfAsync(request, cancellationToken));

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return await ConflictAfterRaceAsync(caller, id, "Someone else changed this request a moment ago.", cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return await ConflictAfterRaceAsync(caller, id, "The driver already has another open request.", cancellationToken);
            }

            await AnnounceAsync(request, decision.Change.Value, caller.UserId, cancellationToken);

            if (decision.NotifyDriver)
                await NotifyDriverClaimedAsync(request, cancellationToken);

            return CallRequestResult<CallRequestSummaryDto>.Ok(await SummaryOfAsync(request, cancellationToken));
        }

        private async Task<CallRequestResult<CallRequestSummaryDto>> ConflictAfterRaceAsync(
            OfficeCaller caller,
            int id,
            string message,
            CancellationToken cancellationToken)
        {
            _context.ChangeTracker.Clear();

            var current = await Scoped(caller).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return current is null
                ? CallRequestResult<CallRequestSummaryDto>.NotFound()
                : CallRequestResult<CallRequestSummaryDto>.Conflict(await SummaryOfAsync(current, cancellationToken), message);
        }

        private sealed record Decision(
            CallRequestEventType? Change,
            CallRequestOutcome Outcome = CallRequestOutcome.Ok,
            string? Message = null,
            bool NotifyDriver = false)
        {
            public static readonly Decision Unchanged = new(Change: null);

            public static Decision Conflict(string message) =>
                new(Change: null, Outcome: CallRequestOutcome.Conflict, Message: message);

            public static Decision Made(CallRequestEventType change, bool notifyDriver = false) =>
                new(change, NotifyDriver: notifyDriver);
        }

        /// <summary>A provider's dispatcher sees their provider's drivers; an internal user sees all.</summary>
        private IQueryable<DriverCallRequest> Scoped(OfficeCaller caller) =>
            caller.ProviderId.HasValue
                ? _context.DriverCallRequests.Where(x => x.ProviderId == caller.ProviderId)
                : _context.DriverCallRequests;

        private static void SetClaim(DriverCallRequest request, OfficeCaller caller, DateTime now)
        {
            request.ClaimedByUserId = caller.UserId;
            request.ClaimedByName = caller.Name;
            request.ClaimedAtUtc = now;
        }

        private static void ClearClaim(DriverCallRequest request)
        {
            request.ClaimedByUserId = null;
            request.ClaimedByName = null;
            request.ClaimedAtUtc = null;
        }

        #endregion

        #region Route context

        private sealed record StopRow(
            int Id,
            int? TripId,
            bool IsPullOut,
            bool IsPullIn,
            ScheduleEventType? EventType,
            int? Sequence,
            TimeSpan? ScheduledPickup,
            TimeSpan? ScheduledAppt,
            TimeSpan? Eta,
            bool Performed,
            TimeSpan? PerformedAt);

        private async Task<CallRequestRouteContextDto> RouteContextAsync(
            int routeId,
            DateTime day,
            int? scheduleAtRequest,
            CancellationToken cancellationToken)
        {
            var start = day.Date;
            var end = start.AddDays(1);

            // Schedule.Name carries the patient's name on ordinary stops. It is compared inside the
            // query to spot Pull-out and Pull-in, and never read out of the database.
            var stops = await _context.Schedules
                .AsNoTracking()
                .Where(s => s.VehicleRouteId == routeId && s.Date >= start && s.Date < end)
                .Select(s => new StopRow(
                    s.Id,
                    s.TripId,
                    s.Name == "Pull-out",
                    s.Name == "Pull-in",
                    s.EventType,
                    s.Sequence,
                    s.ScheduledPickupTime,
                    s.ScheduledApptTime,
                    s.ETATime,
                    s.Performed,
                    s.ActualPerformTime))
                .ToListAsync(cancellationToken);

            var ordered = stops
                .OrderBy(s => s.IsPullOut ? int.MinValue : s.IsPullIn ? int.MaxValue : s.Sequence ?? int.MaxValue - 1)
                .ToList();

            var pullOut = ordered.FirstOrDefault(s => s.IsPullOut);
            var pullIn = ordered.FirstOrDefault(s => s.IsPullIn);
            var work = ordered.Where(s => !s.IsPullOut && !s.IsPullIn).ToList();

            return new CallRequestRouteContextDto
            {
                PulledOut = pullOut?.Performed ?? false,
                PulledOutAt = pullOut?.PerformedAt,
                PulledIn = pullIn?.Performed ?? false,
                StopsDone = work.Count(s => s.Performed),
                StopsTotal = work.Count,
                LastPerformed = Stop(work.LastOrDefault(s => s.Performed)),
                Next = Stop(ordered.FirstOrDefault(s => !s.Performed)),
                AtRequest = scheduleAtRequest is int id ? Stop(ordered.FirstOrDefault(s => s.Id == id)) : null
            };
        }

        private static CallRequestStopDto? Stop(StopRow? row)
        {
            if (row is null)
                return null;

            var kind = row.IsPullOut ? "PullOut"
                : row.IsPullIn ? "PullIn"
                : row.EventType switch
                {
                    ScheduleEventType.Pickup => "Pickup",
                    ScheduleEventType.Dropoff => "Dropoff",
                    _ => "Stop"
                };

            var scheduled = kind switch
            {
                "Pickup" => row.ScheduledPickup,
                "Dropoff" => row.ScheduledAppt,
                _ => null
            };

            int? minutesLate = !row.Performed && row.Eta.HasValue && scheduled.HasValue
                ? (int)Math.Round((row.Eta.Value - scheduled.Value).TotalMinutes)
                : null;

            return new CallRequestStopDto
            {
                ScheduleId = row.Id,
                TripId = row.TripId,
                Kind = kind,
                ScheduledTime = scheduled,
                Eta = row.Eta,
                MinutesLate = minutesLate,
                Performed = row.Performed,
                PerformedAt = row.PerformedAt
            };
        }

        #endregion

        #region Announcing

        /// <summary>Tells every open office screen. Never throws: the change is already saved.</summary>
        private async Task AnnounceAsync(
            DriverCallRequest request,
            CallRequestEventType change,
            int? byUserId,
            CancellationToken cancellationToken)
        {
            try
            {
                var summary = await SummaryOfAsync(request, cancellationToken);

                await _board.CallRequestChangedAsync(
                    new CallRequestChangedMessage
                    {
                        Change = change.ToString(),
                        ByUserId = byUserId,
                        Request = summary
                    },
                    request.ProviderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not announce {Change} for call request {CallRequestId}.", change, request.Id);
            }
        }

        private async Task NotifyDriverClaimedAsync(DriverCallRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _notificationService.PublishAsync(
                    eventCode: BusinessEventCodes.DriverCallRequestClaimed,
                    aggregateId: UserIdentifierConverter.ToGuid(request.Id),
                    data: new Dictionary<string, object?>
                    {
                        [BusinessEventDataKeys.DriverId] = request.DriverId,
                        [BusinessEventDataKeys.CallRequestId] = request.Id
                    },
                    performedByUserId: null,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // A push that does not arrive costs the driver a reassurance, not the case.
                _logger.LogError(ex, "Could not publish DRIVER_CALL_REQUEST_CLAIMED for call request {CallRequestId}.", request.Id);
            }
        }

        #endregion

        #region Mapping

        private async Task<CallRequestSummaryDto> SummaryOfAsync(DriverCallRequest request, CancellationToken cancellationToken)
        {
            var number = await _context.DriverCallRequests
                .AsNoTracking()
                .CountAsync(
                    x => x.DriverId == request.DriverId &&
                         x.OperatingDate == request.OperatingDate &&
                         x.RequestedAtUtc <= request.RequestedAtUtc,
                    cancellationToken);

            return ToSummary(request, Math.Max(1, number));
        }

        private async Task<Dictionary<int, int>> NumbersOfDayAsync(
            IReadOnlyCollection<DriverCallRequest> rows,
            CancellationToken cancellationToken)
        {
            if (rows.Count == 0)
                return new Dictionary<int, int>();

            var driverIds = rows.Select(r => r.DriverId).Distinct().ToList();
            var firstDay = rows.Min(r => r.OperatingDate);
            var lastDay = rows.Max(r => r.OperatingDate);

            var all = await _context.DriverCallRequests
                .AsNoTracking()
                .Where(x => driverIds.Contains(x.DriverId) && x.OperatingDate >= firstDay && x.OperatingDate <= lastDay)
                .Select(x => new { x.Id, x.DriverId, x.OperatingDate, x.RequestedAtUtc })
                .ToListAsync(cancellationToken);

            return all
                .GroupBy(x => (x.DriverId, x.OperatingDate))
                .SelectMany(g => g
                    .OrderBy(x => x.RequestedAtUtc)
                    .ThenBy(x => x.Id)
                    .Select((x, index) => (x.Id, Number: index + 1)))
                .ToDictionary(x => x.Id, x => x.Number);
        }

        private static CallRequestSummaryDto ToSummary(DriverCallRequest request, int numberOfDay) => new()
        {
            Id = request.Id,
            Status = request.Status.ToString(),
            DriverId = request.DriverId,
            DriverName = request.DriverName,
            VehicleRouteId = request.VehicleRouteId,
            RouteName = request.RouteName,
            OperatingDate = request.OperatingDate.Date,
            RequestedAtUtc = Utc(request.RequestedAtUtc),
            QueuedAtUtc = Utc(request.QueuedAtUtc),
            ReminderCount = request.ReminderCount,
            LastReminderAtUtc = Utc(request.LastReminderAtUtc),
            CallAttempts = request.CallAttempts,
            LastAttemptAtUtc = Utc(request.LastAttemptAtUtc),
            DriverAvailableAtUtc = Utc(request.DriverAvailableAtUtc),
            ClaimedByUserId = request.ClaimedByUserId,
            ClaimedByName = request.ClaimedByName,
            ClaimedAtUtc = Utc(request.ClaimedAtUtc),
            ResolvedByUserId = request.ResolvedByUserId,
            ResolvedByName = request.ResolvedByName,
            ClosedAtUtc = Utc(request.ClosedAtUtc),
            ReasonCode = request.ReasonCode,
            RequestNumberOfDay = numberOfDay,
            Revision = RevisionOf(request.RowVersion)
        };

        private static void AddEvent(
            DriverCallRequest request,
            CallRequestEventType type,
            DateTime now,
            int? byUserId,
            string? byName,
            string? detail = null)
        {
            request.Events.Add(new DriverCallRequestEvent
            {
                Type = type,
                AtUtc = now,
                ByUserId = byUserId,
                ByName = Truncate(byName),
                Detail = Truncate(detail)
            });
        }

        /// <summary>SQL Server returns rowversion big-endian; read that way it only ever grows.</summary>
        private static long RevisionOf(byte[]? rowVersion) =>
            rowVersion is { Length: 8 } ? BinaryPrimitives.ReadInt64BigEndian(rowVersion) : 0;

        // The database hands back Unspecified; marked Utc so the JSON carries the "Z" clients need.
        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;

        private static string? Truncate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            return trimmed.Length <= NameMaxLength ? trimmed : trimmed[..NameMaxLength];
        }

        private static string? FirstName(string? name) =>
            name?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is SqlException { Number: 2601 or 2627 };

        #endregion
    }
}

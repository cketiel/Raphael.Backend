using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Raphael.Api.Services;
using Raphael.Notification.Application.Commands.MarkAllNotificationsViewed;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationUnviewed;
using Raphael.Notification.Application.Commands.DeleteSignal;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Notification.Infrastructure.Realtime;
using Raphael.Notification.Infrastructure.Realtime.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;
using Raphael.Shared.Interfaces;
using System.Security.Claims;

using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Api.Controllers
{
    [Authorize] // Requires the driver's JWT
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController : ControllerBase
    {
        /// <summary>
        /// RoleId of an administrator. The token carries role identifiers, not names, so
        /// [Authorize(Roles = "Admin")] would match nobody.
        /// </summary>
        private const string AdminRoleId = "1";

        private readonly IDriverService _driverService;
        private readonly RaphaelContext _context;
        private readonly IFirebaseMessagingService _firebaseMessagingService;
        private readonly ICurrentUserService _currentUser;
        private readonly NotificationRealtimeOptions _realtimeOptions;
        private readonly GetRecipientNotificationsHandler _getRecipientNotificationsHandler;
        private readonly INotificationRecipientRepository _recipientRepository;
        private readonly MarkNotificationViewedHandler _markViewedHandler;
        private readonly MarkNotificationUnviewedHandler _markUnviewedHandler;
        private readonly MarkAllNotificationsViewedHandler _markAllViewedHandler;
        private readonly MarkNotificationAcknowledgedHandler _markAcknowledgedHandler;
        private readonly DeleteSignalHandler _deleteSignalHandler;
        private readonly INotificationDispatcher _dispatcher;
        private readonly IConnectionManager _connectionManager;

        public DriverController(
            IDriverService driverService,
            RaphaelContext context,
            IFirebaseMessagingService firebaseMessagingService,
            ICurrentUserService currentUser,
            IOptions<NotificationRealtimeOptions> realtimeOptions,
            GetRecipientNotificationsHandler getRecipientNotificationsHandler,
            INotificationRecipientRepository recipientRepository,
            MarkNotificationViewedHandler markViewedHandler,
            MarkNotificationUnviewedHandler markUnviewedHandler,
            MarkAllNotificationsViewedHandler markAllViewedHandler,
            MarkNotificationAcknowledgedHandler markAcknowledgedHandler,
            DeleteSignalHandler deleteSignalHandler,
            INotificationDispatcher dispatcher,
            IConnectionManager connectionManager)
        {
            _deleteSignalHandler = deleteSignalHandler;
            _dispatcher = dispatcher;
            _connectionManager = connectionManager;
            _driverService = driverService;
            _context = context;
            _firebaseMessagingService = firebaseMessagingService;
            _currentUser = currentUser;
            _realtimeOptions = realtimeOptions.Value;
            _getRecipientNotificationsHandler = getRecipientNotificationsHandler;
            _recipientRepository = recipientRepository;
            _markViewedHandler = markViewedHandler;
            _markUnviewedHandler = markUnviewedHandler;
            _markAllViewedHandler = markAllViewedHandler;
            _markAcknowledgedHandler = markAcknowledgedHandler;
        }

        #region Push token

        [HttpPost("test-driver-push")]
        public async Task<IActionResult> TestDriverPush([FromQuery] string message)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            if (!int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var user = await _context.Users.FindAsync(currentUserId);

            if (user == null || string.IsNullOrEmpty(user.PushToken))
                return BadRequest("The driver does not have a registered PushToken.");

            var success = await _firebaseMessagingService.SendNotificationToDriverAsync(
                user.PushToken,
                "Test Raphael.Driver",
                message ?? "This is a test notification for the MAUI app.",
                new Dictionary<string, string> { { "test", "true" } }
            );

            return success ? Ok("Notification sent to Firebase") : Problem("Sending to Firebase failed.");
        }

        [HttpPost("push-token")]
        public async Task<IActionResult> UpdateToken([FromBody] string token)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;

            // User.Id is an int. Parsing it as a Guid always failed, so no driver ever
            // managed to register a device and push towards Raphael.Driver never worked.
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _driverService.UpdatePushTokenAsync(userId, token);
            return success ? Ok() : NotFound("User not found.");
        }

        /// <summary>
        /// Forgets this driver's device. Called on sign out.
        /// </summary>
        /// <remarks>
        /// Phones are handed over between shifts. Without this, the next driver to sign in on
        /// the same device keeps receiving the previous one's notifications: trips that are
        /// not theirs, on a screen they did not ask for.
        /// </remarks>
        [HttpDelete("push-token")]
        public async Task<IActionResult> ClearToken()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _driverService.ClearPushTokenAsync(userId);
            return success ? NoContent() : NotFound("User not found.");
        }

        #endregion

        #region Notification Management

        /// <summary>
        /// Inbox of a Raphael.Driver user.
        /// </summary>
        /// <remarks>
        /// Only what is still visible: driver notifications live for twelve hours, one shift,
        /// and so do route signals. See <c>NotificationRetentionPolicy</c>.
        ///
        /// <para>
        /// ⚠️ <b>Signals included</b>, hence the explicit scope. A route signal is not written
        /// for a person to read, but hiding it left the driver with a bell that moved for
        /// reasons they could not see. Until we decide what a driver should be told when their
        /// route changes underneath them, the honest answer is to show it.
        /// </para>
        ///
        /// <para>
        /// The two rules addressed to a driver are <c>TRIP_CANCELLED</c>, only when the trip
        /// was already under way, and the <c>DRIVER_ROUTE_UPDATED</c> signal, only after
        /// Pull-out. An empty list is the normal answer rather than a sign of breakage.
        /// </para>
        /// </remarks>
        [HttpGet("notifications")]
        public async Task<IActionResult> GetMyNotifications(CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var query = new GetRecipientNotificationsQuery(
                RecipientIdOf(driverId),
                RecipientType.Driver,
                NotificationScope.All);

            var result = await _getRecipientNotificationsHandler.Handle(query, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// How many unread notifications the bell should show.
        /// </summary>
        /// <remarks>
        /// Route signals count, the same rows the inbox above returns. A badge and a list that
        /// disagree about the same screen is worse than either number on its own.
        /// </remarks>
        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> GetMyUnreadCount(CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var count = await _recipientRepository.CountUnviewedAsync(
                RecipientIdOf(driverId),
                RecipientType.Driver.Id,
                cancellationToken);

            return Ok(new { count });
        }

        [HttpPost("notifications/{recipientRecordId:guid}/view")]
        public async Task<IActionResult> MarkMyNotificationViewed(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            var owned = await EnsureOwnedAsync(recipientRecordId, cancellationToken);

            if (owned is not null)
                return owned;

            await _markViewedHandler.Handle(
                new MarkNotificationViewedCommand(recipientRecordId),
                cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Puts one notification back in the unread pile.
        /// </summary>
        /// <remarks>
        /// A driver who opens a notice at the wheel cannot act on it right then. Without a way
        /// back, the only thing keeping it from being lost is their memory.
        /// </remarks>
        [HttpPost("notifications/{recipientRecordId:guid}/unview")]
        public async Task<IActionResult> MarkMyNotificationUnviewed(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            var owned = await EnsureOwnedAsync(recipientRecordId, cancellationToken);

            if (owned is not null)
                return owned;

            await _markUnviewedHandler.Handle(
                new MarkNotificationUnviewedCommand(recipientRecordId),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("notifications/{recipientRecordId:guid}/acknowledge")]
        public async Task<IActionResult> MarkMyNotificationAcknowledged(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            var owned = await EnsureOwnedAsync(recipientRecordId, cancellationToken);

            if (owned is not null)
                return owned;

            await _markAcknowledgedHandler.Handle(
                new MarkNotificationAcknowledgedCommand(recipientRecordId),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("notifications/read-all")]
        public async Task<IActionResult> MarkAllMyNotificationsViewed(CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var affected = await _markAllViewedHandler.Handle(
                new MarkAllNotificationsViewedCommand(
                    RecipientIdOf(driverId),
                    RecipientType.Driver),
                cancellationToken);

            return Ok(new { affected });
        }

        #endregion

        // =====================================================================
        // ⚠️⚠️  TEMPORARY TEST ENDPOINTS — RE-002. DELETE BEFORE THE RELEASE. ⚠️⚠️
        // =====================================================================
        //
        // Anonymous on purpose, so a phone can be exercised against the deployed API without
        // carrying a JWT around. That is also exactly what makes them dangerous: anybody who
        // can reach this API can put arbitrary text on a driver's phone. A fake "Trip
        // cancelled" is not spam in this domain — it is a driver abandoning a real pickup.
        //
        // What keeps the blast radius small while they exist:
        //   · every title is forced to start with "TEST", so nothing can pass for dispatch
        //   · the driver has to be named by id: there is no broadcast
        //   · the message never carries patient data, same rule as the real ones
        //
        // Registered for removal in _meta/BACKLOG.md, session `backend-security`, next to the
        // other anonymous test endpoints already open (RiderController, NotificationCatalog).
        //
        #region Temporary test endpoints

        /// <summary>
        /// Sends the driver a real in-app notification: stored, delivered and in their inbox.
        /// </summary>
        /// <remarks>
        /// Not a shortcut through the hub. It writes the notification and its recipient row the
        /// same way the engine does, so what comes back is a genuine test of the whole path:
        /// the live message, the bell counter, the inbox after a refresh, and view/unview on a
        /// recipient id that actually exists.
        ///
        /// <para>
        /// The response reports how many hub connections the driver has open, which is the
        /// first thing worth knowing when a notification does not show up: zero means the app
        /// never reached <c>/hubs/notifications</c>, and no amount of resending will help.
        /// </para>
        /// </remarks>
        [Authorize(Roles = AdminRoleId)]
        [HttpPost("test/inapp/{driverUserId:int}")]
        public async Task<IActionResult> TestInAppNotification(
            int driverUserId,
            [FromQuery] string? message,
            [FromQuery] int? tripId,
            CancellationToken cancellationToken)
        {
            var driver = await _context.Users.FindAsync([driverUserId], cancellationToken);

            if (driver is null)
                return NotFound($"No user with id {driverUserId}.");

            // ⚠️ The role is not a formality here. The hub reads it to decide whether an
            // internal user is a driver or a dispatcher, and registers the connection under a
            // different Guid for each. Send to a user whose role is not in DriverRoleIds and
            // the message goes to a recipient with no connections, while GET
            // /api/driver/notifications answers 403. Both look like "nothing happened".
            var isDriverRole =
                _realtimeOptions.DriverRoleIds.Length == 0 ||
                _realtimeOptions.DriverRoleIds.Contains(driver.RoleId);

            if (!isDriverRole)
            {
                return BadRequest(new
                {
                    error = $"{driver.Username} has role {driver.RoleId}, which is not a driver role.",
                    driverRoleIds = _realtimeOptions.DriverRoleIds,
                    why = "The hub registers this user as a dispatcher, under a different "
                        + "recipient id, and the driver inbox refuses them with 403. "
                        + "Test with a user whose RoleId is in driverRoleIds."
                });
            }

            var recipientId = RecipientIdOf(driverUserId);

            var notification = new NotificationModel(
                businessEventCode: "DRIVER_TEST_NOTIFICATION",
                priority: NotificationPriority.High,
                severity: NotificationSeverity.Information,
                type: NotificationType.Alert,
                title: "TEST — Raphael Driver",
                message: string.IsNullOrWhiteSpace(message)
                    ? "Test notification. If you can read this, the in-app channel works."
                    : $"TEST: {message}",
                // Same twelve hour window as any driver notification, so the test row ages out
                // on its own instead of sitting in the table for ever.
                expiresAtUtc: DateTime.UtcNow.Add(
                    NotificationRetentionPolicy.VisibleFor(RecipientType.Driver)));

            var recipient = new NotificationRecipient(
                notification.Id,
                recipientId,
                RecipientType.Driver);

            recipient.MarkDelivered();

            notification.Recipients.Add(recipient);

            notification.Metadata.Add(
                new NotificationMetadata(
                    notification.Id,
                    NotificationMetadataKeys.MessageKey,
                    "DRIVER_TEST_NOTIFICATION"));

            if (tripId.HasValue)
            {
                notification.Metadata.Add(
                    new NotificationMetadata(
                        notification.Id,
                        NotificationMetadataKeys.TripId,
                        tripId.Value.ToString()));
            }

            await _context.Notifications.AddAsync(notification, cancellationToken);

            // Saved before dispatching: the dispatcher writes a delivery row pointing at this
            // notification, and publishing something that is not yet committed is the mistake
            // the whole engine is built to avoid.
            await _context.SaveChangesAsync(cancellationToken);

            var connections = await _connectionManager.GetUserConnectionsAsync(recipientId);

            await _dispatcher.SendNotificationAsync(
                recipientId,
                RecipientType.Driver,
                ToDto(notification, recipient),
                cancellationToken);

            return Ok(new
            {
                notification.Id,
                recipientRecordId = recipient.Id,
                driver = driver.Username,
                roleId = driver.RoleId,
                hubConnections = connections.Count,
                hint = connections.Count == 0
                    ? "The driver is not connected to the hub. It will show up in the inbox on the next refresh, but nothing arrived live. Check that the app signed in and reached /hubs/notifications."
                    : "Delivered live. It is also in the inbox."
            });
        }

        /// <summary>
        /// Sends the driver a push through Firebase, carrying the deep link data.
        /// </summary>
        /// <remarks>
        /// Unlike <c>test-driver-push</c>, this one needs no token of its own and includes the
        /// extras the app reads to open the notifications page, so it exercises the tap as well
        /// as the delivery. Close the app before calling it: a push that arrives with the app
        /// in the foreground proves less than one that wakes it.
        /// </remarks>
        [Authorize(Roles = AdminRoleId)]
        [HttpPost("test/push/{driverUserId:int}")]
        public async Task<IActionResult> TestPushNotification(
            int driverUserId,
            [FromQuery] string? message,
            [FromQuery] int? tripId,
            CancellationToken cancellationToken)
        {
            var driver = await _context.Users.FindAsync([driverUserId], cancellationToken);

            if (driver is null)
                return NotFound($"No user with id {driverUserId}.");

            if (string.IsNullOrEmpty(driver.PushToken))
            {
                return BadRequest(
                    $"{driver.Username} has no push token registered. Sign in on the phone " +
                    "and accept the notification permission first.");
            }

            // Identifiers only, never patient data: a push crosses Google's servers and ends up
            // written on a lock screen. Same payload shape PushSender builds for the real ones.
            var data = new Dictionary<string, string>
            {
                ["notificationId"] = Guid.NewGuid().ToString(),
                ["businessEventCode"] = "DRIVER_TEST_NOTIFICATION"
            };

            if (tripId.HasValue)
                data["tripId"] = tripId.Value.ToString();

            var sent = await _firebaseMessagingService.SendNotificationToDriverAsync(
                driver.PushToken,
                "TEST — Raphael Driver",
                string.IsNullOrWhiteSpace(message)
                    ? "Test push. Tap it: the app should open on the notifications screen."
                    : $"TEST: {message}",
                data);

            if (!sent)
            {
                // Firebase rejects a token that belongs to another project, or one the device
                // has since rotated. Both look identical from here.
                return Problem(
                    "Firebase refused the message. Either the token is stale — sign out and " +
                    "back in on the phone — or google-services.json does not come from the " +
                    "raphael-nemt project.");
            }

            return Ok(new
            {
                driver = driver.Username,
                tokenPreview = driver.PushToken.Length > 12
                    ? $"{driver.PushToken[..12]}…"
                    : "(short)",
                data,
                hint = "Sent to Firebase. Delivery to the handset is Firebase's call from here."
            });
        }

        /// <summary>
        /// Shapes a freshly created notification the way the inbox query would return it.
        /// </summary>
        /// <remarks>
        /// The recipient row has to travel with it: the app reads the identifier for view and
        /// unview from there, and a live notification without it arrives unmarkable.
        /// </remarks>
        private static NotificationDto ToDto(
            NotificationModel notification,
            NotificationRecipient recipient)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                BusinessEventCode = notification.BusinessEventCode,
                Priority = notification.Priority.Name,
                Severity = notification.Severity.Name,
                Type = notification.Type.Name,
                Status = notification.Status.Name,
                Title = notification.Title,
                Message = notification.Message,
                CreatedAtUtc = notification.CreatedAtUtc,
                ExpiresAtUtc = notification.ExpiresAtUtc,
                Recipients =
                [
                    new NotificationRecipientDto
                    {
                        Id = recipient.Id,
                        RecipientId = recipient.RecipientId,
                        RecipientType = recipient.RecipientType.Name,
                        IsBroadcast = false,
                        Status = recipient.Status.Name,
                        DeliveredAtUtc = recipient.DeliveredAtUtc,
                        ViewedAtUtc = recipient.ViewedAtUtc,
                        AcknowledgedAtUtc = recipient.AcknowledgedAtUtc
                    }
                ],
                Metadata = notification.Metadata.ToDictionary(m => m.Key, m => m.Value)
            };
        }

        #endregion

        #region Signals

        /// <summary>
        /// Route changes waiting to be acted on. Never notices.
        /// </summary>
        /// <remarks>
        /// A signal says the schedule the app has on screen is out of date. It is delivered
        /// live over the hub; this endpoint exists so an app that was closed, or whose socket
        /// was down, can drain whatever it missed when it comes back.
        ///
        /// <para>
        /// This is the signals on their own. <c>GET notifications</c> returns them too, mixed
        /// with the notices, because the bell shows them today; an app draining what it missed
        /// wants only these, and asking for the whole inbox to filter it again would move a
        /// server decision back into every client.
        /// </para>
        /// </remarks>
        [HttpGet("notifications/signals")]
        public async Task<IActionResult> GetMySignals(CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var query = new GetRecipientNotificationsQuery(
                RecipientIdOf(driverId),
                RecipientType.Driver,
                NotificationScope.Signals);

            var result = await _getRecipientNotificationsHandler.Handle(query, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a signal the application has already acted on.
        /// </summary>
        /// <remarks>
        /// ⚠️ Signals only. A notice is refused, and that is the point: a cancellation is a
        /// record somebody may have to answer for later, and no driver tidying their screen
        /// should be able to destroy one. What a driver does not want to see, the app hides on
        /// the device; deleting notices belongs to the retention policy alone.
        ///
        /// <para>
        /// ⚠️ <b>Raphael.Driver stopped calling this.</b> A signal is shown in the bell now, so
        /// deleting it the moment the app acted on it would take a row off a list the driver
        /// had not read yet. The app remembers on the device which signals it already acted on,
        /// and the row ages out on its own. Kept here because it is the only safe way to remove
        /// one, and the next client that needs it will need it to exist.
        /// </para>
        /// </remarks>
        [HttpDelete("notifications/signals/{recipientRecordId:guid}")]
        public async Task<IActionResult> DeleteMySignal(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var deleted = await _deleteSignalHandler.Handle(
                new DeleteSignalCommand(
                    recipientRecordId,
                    RecipientIdOf(driverId),
                    RecipientType.Driver),
                cancellationToken);

            return deleted ? NoContent() : NotFound();
        }

        #endregion

        #region Identity of the caller

        /// <summary>
        /// The driver behind the token, or nothing when the caller is not one.
        /// </summary>
        /// <remarks>
        /// Drivers and dispatchers are both rows of the Users table, so the role decides which
        /// application is calling. The list of driver roles is read from the very same options
        /// the notification hub uses, so the API and the hub cannot end up disagreeing about
        /// who is a driver — which would put dispatch notices in a driver's inbox.
        ///
        /// <para>
        /// While the list is empty every internal user counts as a driver: the least privileged
        /// of the two, and the same default the hub takes.
        /// </para>
        /// </remarks>
        private bool TryGetDriverId(out int driverId)
        {
            driverId = 0;

            var userId = _currentUser.UserId;

            if (!userId.HasValue || userId.Value <= 0)
                return false;

            if (_realtimeOptions.DriverRoleIds.Length > 0 && !IsDriverRole())
                return false;

            driverId = userId.Value;

            return true;
        }

        private bool IsDriverRole()
        {
            return User
                .FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("Role"))
                .Any(claim =>
                    int.TryParse(claim.Value, out var roleId) &&
                    _realtimeOptions.DriverRoleIds.Contains(roleId));
        }

        private static Guid RecipientIdOf(int driverId)
            => UserIdentifierConverter.ToGuid(driverId, RecipientType.Driver);

        /// <summary>
        /// Returns null when the row belongs to the caller, or the response to send back when
        /// it does not.
        /// </summary>
        /// <remarks>
        /// ⚠️ Without this, any signed in driver could mark somebody else's notifications as
        /// read by guessing an identifier, and a cancellation nobody saw is a vehicle driving
        /// to a pickup that no longer exists. A row that belongs to someone else is reported
        /// as missing rather than as forbidden: confirming it exists would already say more
        /// than the caller is entitled to know.
        /// </remarks>
        private async Task<IActionResult?> EnsureOwnedAsync(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            if (!TryGetDriverId(out var driverId))
                return Forbid();

            var recipient = await _recipientRepository.GetByIdAsync(
                recipientRecordId,
                cancellationToken);

            if (recipient is null)
                return NotFound();

            if (recipient.RecipientId != RecipientIdOf(driverId) ||
                recipient.RecipientTypeId != RecipientType.Driver.Id)
            {
                return NotFound();
            }

            return null;
        }

        #endregion
    }
}

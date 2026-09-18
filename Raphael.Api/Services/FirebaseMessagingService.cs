using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Raphael.Api.Services
{
    /// <summary>
    /// Sends push notifications to drivers through Firebase Cloud Messaging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ Every failure in here used to be silent, and that is what this class is shaped to
    /// prevent. Without a credential the constructor simply did not create a
    /// <see cref="FirebaseApp"/>; the send then threw on <c>DefaultInstance</c> and was
    /// swallowed by a bare <c>catch</c>. The API started clean, answered its health probe, and
    /// drivers stopped being told about their trips without one line anywhere saying so.
    /// </para>
    /// <para>
    /// So: the constructor states which credential it used, or says at Error level that it has
    /// none. It does not throw — Firebase being unreachable is not a reason to take down
    /// dispatch, billing and the driver app with it — but it is no longer quiet.
    /// </para>
    /// <para>
    /// ⚠️ It is registered as a singleton and singletons are built on first use, so none of
    /// that would be said at startup either. <c>Program.cs</c> resolves it once while the app
    /// boots, on purpose, so the answer arrives with the rest of the startup log instead of
    /// with the first driver who happens to need a notification.
    /// </para>
    /// </remarks>
    public class FirebaseMessagingService : IFirebaseMessagingService
    {
        /// <summary>Configuration key holding the service account JSON as a single string.</summary>
        /// <remarks>
        /// In Azure this arrives as the <c>Firebase__ServiceAccountJson</c> app setting, backed by
        /// Key Vault. ⚠️ A secret resolving is not the same as a secret being complete: a
        /// truncated value gets here as malformed JSON, which is why parsing is reported.
        /// </remarks>
        public const string ServiceAccountJsonKey = "Firebase:ServiceAccountJson";

        /// <summary>File the credential falls back to, next to the application.</summary>
        /// <remarks>
        /// ⚠️ Measured on 2026-09-17: a local <c>dotnet publish</c> carries this file and a CI
        /// build does not, because it is in <c>.gitignore</c>. The fallback that keeps MyASP.NET
        /// working therefore disappears exactly when deployment moves to GitHub Actions, which
        /// is why using it is reported as a warning rather than treated as normal.
        /// </remarks>
        public const string ServiceAccountFileName = "firebase-adminsdk.json";

        private readonly ILogger<FirebaseMessagingService> _logger;

        /// <summary>
        /// Whether a <see cref="FirebaseApp"/> is available to send with.
        /// </summary>
        public bool IsConfigured => FirebaseApp.DefaultInstance != null;

        public FirebaseMessagingService(
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<FirebaseMessagingService> logger)
        {
            _logger = logger;

            if (FirebaseApp.DefaultInstance != null)
            {
                return;
            }

            var json = config[ServiceAccountJsonKey];

            if (!string.IsNullOrWhiteSpace(json))
            {
                Create(() => GoogleCredential.FromJson(json), $"configuration key {ServiceAccountJsonKey}");
                return;
            }

            var path = Path.Combine(env.ContentRootPath, ServiceAccountFileName);

            if (File.Exists(path))
            {
                _logger.LogWarning(
                    "No {Key} is configured, so Firebase is falling back to the {File} file. " +
                    "That file is not carried by a CI publish: set the configuration key before " +
                    "deploying from source control.",
                    ServiceAccountJsonKey,
                    ServiceAccountFileName);

                Create(() => GoogleCredential.FromFile(path), $"file {ServiceAccountFileName}");
                return;
            }

            // Not an exception, and deliberately so: the API can serve dispatch, trips and
            // billing without Firebase. What it cannot do is pretend this is normal.
            _logger.LogError(
                "Firebase has no credential: neither {Key} nor {File} is present. " +
                "Drivers will not receive push notifications until one of them is.",
                ServiceAccountJsonKey,
                ServiceAccountFileName);
        }

        private void Create(Func<GoogleCredential> credential, string source)
        {
            try
            {
                FirebaseApp.Create(new AppOptions { Credential = credential() });

                _logger.LogInformation(
                    "Firebase initialised from {Source}. Driver push notifications are enabled.",
                    source);
            }
            catch (Exception ex)
            {
                // Most often a truncated or malformed secret. Said out loud, because the
                // alternative is discovering it from a driver who never got a trip.
                _logger.LogError(
                    ex,
                    "Firebase credential from {Source} could not be read. Drivers will not " +
                    "receive push notifications.",
                    source);
            }
        }

        public async Task<bool> SendNotificationToDriverAsync(
            string fcmToken,
            string title,
            string body,
            Dictionary<string, string>? data = null)
        {
            if (string.IsNullOrEmpty(fcmToken))
            {
                _logger.LogWarning("Push notification skipped: the recipient has no device token.");
                return false;
            }

            if (!IsConfigured)
            {
                _logger.LogError(
                    "Push notification not sent: Firebase was never initialised. See the startup log.");
                return false;
            }

            var message = new Message
            {
                Token = fcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
                Data = data, // Útil para mandar TripId
                Android = new AndroidConfig { Priority = Priority.High },
                Apns = new ApnsConfig { Headers = new Dictionary<string, string> { { "apns-priority", "10" } } }
            };

            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return true;
            }
            catch (FirebaseMessagingException ex)
            {
                // Firebase answered and refused. Usually a token belonging to an app that was
                // uninstalled or reinstalled. The token itself is not logged: it identifies a
                // device, and device identifiers do not belong in a log.
                _logger.LogWarning(
                    ex,
                    "Firebase rejected a push notification with {ErrorCode}.",
                    ex.MessagingErrorCode);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push notification failed before Firebase could answer.");
                return false;
            }
        }
    }
}

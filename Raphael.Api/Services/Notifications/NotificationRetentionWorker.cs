using Raphael.Notification.Application.Services;

namespace Raphael.Api.Services.Notifications
{
    /// <summary>
    /// Runs the notification cleanup once a night.
    /// </summary>
    /// <remarks>
    /// Automatic rather than a button somebody remembers to press: the table grows every
    /// day whether or not anybody is watching. The same work is also exposed as an admin
    /// endpoint, for when it needs running out of turn.
    ///
    /// <para>
    /// Deliberately not a fixed clock time. The interval is long enough that catching up
    /// a few hours late costs nothing, and starting the pass on boot means a server that
    /// was down over the weekend cleans up as soon as it returns.
    /// </para>
    /// </remarks>
    public class NotificationRetentionWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

        private readonly IServiceProvider _services;
        private readonly ILogger<NotificationRetentionWorker> _logger;

        public NotificationRetentionWorker(
            IServiceProvider services,
            ILogger<NotificationRetentionWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the application finish starting before touching the database.
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();

                    var retention = scope.ServiceProvider
                        .GetRequiredService<NotificationRetentionService>();

                    await retention.RunAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Housekeeping. A bad night must not take the API down; the next pass
                    // picks up everything this one left behind.
                    _logger.LogError(ex, "Notification retention pass failed.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}

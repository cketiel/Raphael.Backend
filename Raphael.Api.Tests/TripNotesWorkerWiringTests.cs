using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raphael.Api.Services.Places;
using Raphael.Shared.Time;

namespace Raphael.Api.Tests
{
    /// <summary>
    /// The notes worker must be constructible the way the API builds it at start-up (RE-054).
    /// </summary>
    /// <remarks>
    /// The first version took the scoped IOperationClock in this singleton: the build in DEV failed
    /// at start-up and the site went down. The API validates scopes on build; this does the same.
    /// </remarks>
    public class TripNotesWorkerWiringTests
    {
        private sealed class AnyClock : IOperationClock
        {
            public DateTime UtcNow => DateTime.UtcNow;
            public TimeZoneInfo ZoneFor(int? providerId) => TimeZoneInfo.Utc;
            public DateTime NowFor(int? providerId) => UtcNow;
            public DateTime TodayFor(int? providerId) => UtcNow.Date;
            public TimeSpan TimeOfDayFor(int? providerId) => UtcNow.TimeOfDay;
            public DateTime ToOperation(DateTime utc, int? providerId) => utc;
            public void InvalidateZones() { }
        }

        [Fact]
        public void The_worker_builds_with_scope_validation_on()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<NotesOptions>();
            services.AddScoped<IOperationClock, AnyClock>(); // scoped, as in Program.cs
            services.AddSingleton<TripNotesQueue>();
            services.AddSingleton<ITripNotesQueue>(sp => sp.GetRequiredService<TripNotesQueue>());
            services.AddHostedService<TripNotesWorker>();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            Assert.Single(provider.GetServices<IHostedService>().OfType<TripNotesWorker>());
        }
    }
}

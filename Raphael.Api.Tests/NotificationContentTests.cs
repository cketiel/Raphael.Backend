using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Domain.Content;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities;

namespace Raphael.Api.Tests
{
    /// <summary>
    /// The text the office and the driver read for the events changed since 2.2.0. Each of these
    /// failed before its fix: TRIP_ASSIGNED fell through to the rule's description, and UPDATED told
    /// the driver a trip had left a route it was still on.
    /// </summary>
    public class NotificationContentTests
    {
        private static NotificationContent Build(string eventCode, RecipientType audience, Action<Dictionary<string, object?>> fill)
        {
            var context = new BusinessEventContext { EventCode = eventCode };
            fill(context.Data);
            // The rule is only read by events with no text of their own; these all have one.
            return NotificationContentBuilder.Build(eventCode, audience, context, rule: null!, TimeZoneInfo.Utc);
        }

        private static void Trip(Dictionary<string, object?> data) =>
            data[BusinessEventDataKeys.Trip] = new Trip { Id = 27138, Date = new DateTime(2026, 10, 20), FromTime = new TimeSpan(9, 15, 0) };

        [Fact]
        public void A_trip_given_to_a_company_says_so_and_what_to_do()
        {
            var content = Build(BusinessEventCodes.TripAssigned, RecipientType.DesktopUser, Trip);

            Assert.Equal("Trip Assigned", content.Title);
            Assert.Contains("27138", content.Message);
            Assert.Contains("Accept it or reject it", content.Message);
        }

        [Fact]
        public void A_route_that_changed_does_not_tell_the_driver_the_trip_is_gone()
        {
            var content = Build(BusinessEventCodes.DriverRouteUpdated, RecipientType.Driver, d =>
            {
                Trip(d);
                d[BusinessEventDataKeys.RouteChange] = RouteChangeTypes.Updated;
            });

            Assert.DoesNotContain("no longer on your route", content.Message);
            Assert.Contains("changed", content.Message);
        }

        [Fact]
        public void A_trip_taken_off_still_says_it_is_gone()
        {
            var content = Build(BusinessEventCodes.DriverRouteUpdated, RecipientType.Driver, d =>
            {
                Trip(d);
                d[BusinessEventDataKeys.RouteChange] = RouteChangeTypes.Removed;
            });

            Assert.Contains("no longer on your route", content.Message);
        }

        [Fact]
        public void Out_of_service_names_the_subjects_and_counts_and_nothing_else()
        {
            var content = Build(BusinessEventCodes.RouteOutOfService, RecipientType.DesktopUser, d =>
            {
                d[BusinessEventDataKeys.VehicleName] = "Van 12";
                d[BusinessEventDataKeys.DriverName] = "Driver A";
                d[BusinessEventDataKeys.RouteNames] = "R-07";
                d[BusinessEventDataKeys.VehicleReasonCode] = "MECHANICAL_FAILURE";
                d[BusinessEventDataKeys.TripsUnrouted] = 4;
                d[BusinessEventDataKeys.TripsOnboard] = 1;
            });

            Assert.Equal("Route Out of Service", content.Title);
            Assert.Contains("Vehicle Van 12 and driver Driver A", content.Message);
            Assert.Contains("4 trip(s) went back", content.Message);
            Assert.Contains("1 trip(s) with a patient on board", content.Message);
            Assert.Equal("MECHANICAL_FAILURE", content.Parameters[NotificationMetadataKeys.VehicleReasonCode]);
            Assert.False(content.Parameters.ContainsKey(NotificationMetadataKeys.TripId));
        }

        [Fact]
        public void Back_in_service_names_the_subject()
        {
            var content = Build(BusinessEventCodes.RouteReturnedToService, RecipientType.DesktopUser, d =>
                d[BusinessEventDataKeys.VehicleName] = "Van 12");

            Assert.Equal("Back in Service", content.Title);
            Assert.StartsWith("Vehicle Van 12", content.Message);
        }
    }
}

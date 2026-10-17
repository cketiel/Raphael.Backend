using Raphael.Api.Services.OutOfService;
using Raphael.Shared.Definitions.OutOfService;

namespace Raphael.Api.Tests
{
    /// <summary>The rules of an Out of Service that do not need a database (RE-053).</summary>
    public class OutOfServiceRulesTests
    {
        [Fact]
        public void A_finished_trip_is_not_touched() =>
            Assert.Null(OutOfServiceService.Classify(pickupPerformed: true, dropoffPerformed: true, routeSubstituted: false));

        [Fact]
        public void A_pending_trip_goes_back_to_the_unscheduled_list() =>
            Assert.Equal(OutOfServiceTripOutcomes.Unrouted,
                OutOfServiceService.Classify(pickupPerformed: false, dropoffPerformed: false, routeSubstituted: false));

        [Fact]
        public void A_patient_on_board_keeps_the_trip_on_the_route() =>
            Assert.Equal(OutOfServiceTripOutcomes.ExcludedOnboard,
                OutOfServiceService.Classify(pickupPerformed: true, dropoffPerformed: false, routeSubstituted: false));

        [Fact]
        public void A_patient_on_board_wins_over_a_substitution() =>
            Assert.Equal(OutOfServiceTripOutcomes.ExcludedOnboard,
                OutOfServiceService.Classify(pickupPerformed: true, dropoffPerformed: false, routeSubstituted: true));

        [Fact]
        public void A_substituted_route_keeps_its_pending_trips() =>
            Assert.Equal(OutOfServiceTripOutcomes.KeptBySubstitution,
                OutOfServiceService.Classify(pickupPerformed: false, dropoffPerformed: false, routeSubstituted: true));

        [Theory]
        [InlineData(OutOfServiceSubjects.Vehicle, OutOfServiceReasonCodes.MechanicalFailure, true)]
        [InlineData(OutOfServiceSubjects.Vehicle, OutOfServiceReasonCodes.MedicalOrPersonalLeave, false)]
        [InlineData(OutOfServiceSubjects.Driver, OutOfServiceReasonCodes.MedicalOrPersonalLeave, true)]
        [InlineData(OutOfServiceSubjects.Driver, OutOfServiceReasonCodes.LiftOrRampFailure, false)]
        [InlineData(OutOfServiceSubjects.Driver, OutOfServiceReasonCodes.Other, true)]
        [InlineData(OutOfServiceSubjects.Driver, "SICK", false)]
        [InlineData("ROUTE", OutOfServiceReasonCodes.Other, false)]
        public void A_reason_belongs_to_its_subject(string subject, string code, bool valid) =>
            Assert.Equal(valid, OutOfServiceReasonCodes.IsValid(subject, code));
    }
}

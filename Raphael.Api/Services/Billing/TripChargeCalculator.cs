using Raphael.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raphael.Api.Services.Billing
{
    /// <summary>
    /// What a trip costs, from the billing rules of its funding source and space type.
    /// </summary>
    /// <remarks>
    /// ⚠️ This is the only place the figure is worked out. It lived inline inside the production
    /// report, which was the only screen that ever showed it; the Home tab now shows a total for
    /// the day, and a second copy of a money formula is the one kind of duplication that nobody
    /// notices going wrong — the two screens simply disagree about an invoice.
    ///
    /// It does not read <c>Trip.Charge</c>. That column exists on the entity and **nothing in the
    /// system has ever written to it**, which is why the Charge column of the trips grid has
    /// always been blank.
    /// </remarks>
    public static class TripChargeCalculator
    {
        /// <summary>
        /// The charge for one trip: the loading fee, plus the miles beyond whatever the funding
        /// source allows for free.
        /// </summary>
        /// <param name="rules">
        /// The billing rules already narrowed to this trip's funding source and space type.
        /// </param>
        /// <param name="distanceMiles">The trip's distance. A trip with no distance bills no miles.</param>
        public static decimal For(IEnumerable<FundingSourceBillingItem> rules, double? distanceMiles)
        {
            if (rules == null) return 0m;

            var applicable = rules as IList<FundingSourceBillingItem> ?? rules.ToList();
            var total = 0m;

            // A. Loading fee / pick up fee — charged once, whatever the distance.
            var loadingFee = applicable.FirstOrDefault(r =>
                Describes(r, "Loading Fee") || Describes(r, "PICK UP"));

            if (loadingFee != null) total += loadingFee.Rate;

            // B. Miles, less the free allowance. Max(0, …) because a funding source that allows
            // more free miles than the trip covers owes nothing back.
            var miles = applicable.FirstOrDefault(r => Describes(r, "MILES"));

            if (miles != null)
            {
                var free = miles.FreeQty ?? 0;
                var billable = Math.Max(0, (distanceMiles ?? 0) - free);

                total += (decimal)billable * miles.Rate;
            }

            return total;
        }

        /// <summary>
        /// Narrows a set of rules to one trip and prices it.
        /// </summary>
        public static decimal For(
            IEnumerable<FundingSourceBillingItem> allRules,
            int? fundingSourceId,
            int spaceTypeId,
            double? distanceMiles)
        {
            if (allRules == null || fundingSourceId == null) return 0m;

            return For(
                allRules.Where(r => r.FundingSourceId == fundingSourceId && r.SpaceTypeId == spaceTypeId),
                distanceMiles);
        }

        private static bool Describes(FundingSourceBillingItem rule, string word) =>
            rule.BillingItem?.Description != null &&
            rule.BillingItem.Description.Contains(word, StringComparison.OrdinalIgnoreCase);
    }
}

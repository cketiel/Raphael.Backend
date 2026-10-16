using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Notifications;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services.Trips
{
    /// <summary>The caller may not accept or reject trips, or not this one: answered 404.</summary>
    public sealed class TripCommitmentDeniedException : Exception
    {
    }

    /// <summary>What was asked is not possible for this trip: answered 400 with the reason.</summary>
    public sealed class TripCommitmentRuleException : Exception
    {
        public TripCommitmentRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// Accepting and rejecting a trip given to someone (PROVIDER_ACCESS_MODEL.md §3.7): the same
    /// rules from the Booking portal (external Providers) and from the Desktop (internal Providers
    /// and the office's admins).
    /// </summary>
    /// <remarks>
    /// Who acts on which trips:
    /// - A Provider's user, external or internal, on its Provider's trips.
    /// - An admin of the office (role 1, no Integrator, no Provider) on Raphael's own trips, the
    ///   ones with no Provider: a clinic may give a trip to Raphael, and Raphael may turn it down.
    /// Anyone else, a clinic included, gets a 404.
    /// Only trips that came from an integration (Booking, the API) are accepted or rejected: a manual
    /// or imported trip has nobody on the other side to tell (user decision, 2026-10-16).
    /// </remarks>
    public interface ITripCommitmentService
    {
        /// <summary>Assigned → Accepted for the ones in the caller's scope. Answers how many.</summary>
        Task<int> AcceptAsync(IReadOnlyCollection<int> tripIds, CancellationToken ct);

        /// <summary>Assigned or Accepted → Rejected, with the reason; TRIP_REJECTED goes out.</summary>
        Task RejectAsync(int tripId, string reason, CancellationToken ct);
    }

    public sealed class TripCommitmentService : ITripCommitmentService
    {
        private const int AdminRoleId = 1;
        private const int MaxReasonLength = 500;
        private const int MaxBatch = 500;

        /// <summary>
        /// What may still be turned down: a trip given, or one accepted that cannot be done after
        /// all. Saying so is better than not showing up.
        /// </summary>
        private static readonly string[] Rejectable = { TripStatus.Assigned, TripStatus.Accepted };

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ITripNotificationPublisher _notifications;

        public TripCommitmentService(RaphaelContext context, ICurrentUserService currentUser, ITripNotificationPublisher notifications)
        {
            _context = context;
            _currentUser = currentUser;
            _notifications = notifications;
        }

        /// <summary>The caller's Provider, or null for an office admin acting for Raphael. Anyone else is denied.</summary>
        private int? Scope()
        {
            if (_currentUser.ProviderId is int providerId) return providerId;
            if (_currentUser.IsMilanesInternal && _currentUser.RoleId == AdminRoleId) return null;
            throw new TripCommitmentDeniedException();
        }

        private IQueryable<Trip> InScope(int? providerId) =>
            providerId is int id ? _context.Trips.Where(t => t.ProviderId == id) : _context.Trips.Where(t => t.ProviderId == null);

        private string Who => _currentUser.ProviderId is null
            ? $"Office - {_currentUser.UserName ?? "admin"}"
            : $"Provider - {_currentUser.UserName ?? "user"}";

        public async Task<int> AcceptAsync(IReadOnlyCollection<int> tripIds, CancellationToken ct)
        {
            var scope = Scope();
            if (tripIds.Count == 0) return 0;
            if (tripIds.Count > MaxBatch) throw new TripCommitmentRuleException($"Accept at most {MaxBatch} trips at a time.");

            var trips = await InScope(scope)
                .Where(t => tripIds.Contains(t.Id) && !t.IsCancelled && t.IntegratorId != null && t.Status == TripStatus.Assigned)
                .ToListAsync(ct);
            foreach (var trip in trips)
            {
                trip.Status = TripStatus.Accepted;
                History(trip, TripStatus.Assigned, TripStatus.Accepted);
            }
            await _context.SaveChangesAsync(ct);
            return trips.Count;
        }

        public async Task RejectAsync(int tripId, string reason, CancellationToken ct)
        {
            var scope = Scope();
            var text = reason?.Trim() ?? "";
            if (text.Length == 0) throw new TripCommitmentRuleException("A reason is required.");
            if (text.Length > MaxReasonLength) throw new TripCommitmentRuleException($"The reason can have at most {MaxReasonLength} characters.");

            var trip = await InScope(scope).FirstOrDefaultAsync(t => t.Id == tripId, ct)
                ?? throw new TripCommitmentDeniedException();
            if (trip.IntegratorId == null)
                throw new TripCommitmentRuleException("This trip did not come from an integration: there is nobody to tell, so it cannot be rejected.");
            if (trip.IsCancelled || !Rejectable.Contains(trip.Status))
                throw new TripCommitmentRuleException("This trip has already been routed, started or closed: it can no longer be rejected.");

            var prior = trip.Status;
            trip.Status = TripStatus.Rejected;
            trip.ProviderOutcome = ProviderOutcome.Rejected;
            trip.ProviderOutcomeReason = text;
            trip.ProviderOutcomeAtUtc = DateTime.UtcNow;
            trip.ProviderOutcomeByUserId = _currentUser.UserId;
            History(trip, prior, $"{TripStatus.Rejected}: {text}");
            await _context.SaveChangesAsync(ct);

            var name = scope is int id
                ? await _context.Providers.Where(p => p.Id == id).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "Its provider"
                : "Raphael";
            await _notifications.TripRejectedAsync(trip, name, text, ct);
        }

        private void History(Trip trip, string? prior, string next) =>
            _context.TripHistories.Add(new TripHistory
            {
                TripId = trip.Id,
                User = Who,
                Field = "Status",
                PriorValue = prior,
                NewValue = next,
                ChangeDate = DateTime.UtcNow
            });
    }
}

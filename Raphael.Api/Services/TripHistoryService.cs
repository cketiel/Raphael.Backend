using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public class TripHistoryService : ITripHistoryService
    {
        private readonly RaphaelContext _context;
        public TripHistoryService(RaphaelContext context) 
        {
            _context = context;
        }

        public async Task<IEnumerable<TripHistory>> GetHistoryByTrip(int tripId)
        {
            return await _context.TripHistories
                .Where(h => h.TripId == tripId)
                .OrderByDescending(h => h.ChangeDate) // Most recent first
                .ToListAsync();
           
        }
        public async Task<TripHistory> PostHistory(TripHistory history)
        {
            _context.TripHistories.Add(history);
            await _context.SaveChangesAsync();
            return history;
        }

        private async Task SaveHistory(int tripId, string user, string field, string priorVal, string newVal)
        {
            var history = new TripHistory
            {
                TripId = tripId,
                User = user,
                Field = field,
                PriorValue = priorVal,
                NewValue = newVal,
                ChangeDate = DateTime.Now
            };
            _context.TripHistories.Add(history);
            await _context.SaveChangesAsync();
        }

    }
}

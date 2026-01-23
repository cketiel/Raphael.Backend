using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface ITripHistoryService
    {  
        Task<IEnumerable<TripHistory>> GetHistoryByTrip(int tripId);
        Task<TripHistory> PostHistory(TripHistory history);
    }
}

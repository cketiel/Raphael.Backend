using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface ITripHistoryService
    {  
        Task<ActionResult<IEnumerable<TripHistory>>> GetHistoryByTrip(int tripId);
        Task<ActionResult<TripHistory>> PostHistory(TripHistory history);
    }
}

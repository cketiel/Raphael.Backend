using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IGpsService
    {
        Task SaveGpsDataAsync(GpsDataDto gpsDataDto);
    }
}

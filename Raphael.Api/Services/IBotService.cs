namespace Raphael.Api.Services
{
    public interface IBotService
    {
        Task<string> ActivateWillCallAsync(string tripNumber);
        Task<string> CancelTripAsync(string tripNumber);
        Task<TimeSpan?> GetEtaAsync(string tripNumber);
        Task<TimeSpan?> GetEtaAsync(string? patientFullName, string? phone, DateTime? date, string? tripNumber);
    }
}

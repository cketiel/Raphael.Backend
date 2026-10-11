namespace Raphael.Api.Services
{
    public interface IBotService
    {
        Task<string> ActivateWillCallAsync(string tripNumber);
        Task<string> CancelTripAsync(string tripNumber);
        Task<TimeSpan?> GetEtaAsync(string tripNumber);
        Task<TimeSpan?> GetEtaAsync(string? patientFullName, string? phone, DateTime? date, string? tripNumber);

        /// <summary>
        /// Why <see cref="GetEtaAsync(string?, string?, DateTime?, string?)"/> found no ETA, as one of
        /// the <see cref="BotEtaReasons"/> codes. Only meaningful after a lookup returned null.
        /// </summary>
        Task<string> GetEtaUnavailableReasonAsync(string? patientFullName, string? phone, DateTime? date, string? tripNumber);
    }
}

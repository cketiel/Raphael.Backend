using Raphael.Shared.DTOs.CallRequests;

namespace Raphael.Api.Services.CallRequests
{
    /// <summary>
    /// Drivers asking the office to call them back, and the office working through those requests.
    /// </summary>
    public interface ICallRequestService
    {
        // ----- Driver -----

        Task<DriverCallRequestDto> GetDriverStateAsync(int driverId, CancellationToken cancellationToken = default);

        /// <summary>Opens a case, or counts a reminder on the one already open.</summary>
        Task<DriverCallRequestDto> RequestOrRemindAsync(
            int driverId,
            CreateCallRequestDto input,
            CancellationToken cancellationToken = default);

        /// <summary>"I can talk now", after the office tried and could not reach the driver.</summary>
        Task<CallRequestResult<DriverCallRequestDto>> DriverAvailableAsync(
            int driverId,
            int requestId,
            CancellationToken cancellationToken = default);

        Task<CallRequestResult<DriverCallRequestDto>> CancelByDriverAsync(
            int driverId,
            int requestId,
            CancellationToken cancellationToken = default);

        // ----- Office -----

        Task<OfficeCaller> ResolveOfficeCallerAsync(
            int userId,
            int? providerId,
            CancellationToken cancellationToken = default);

        /// <summary>Everything open, from any day, plus what closed today.</summary>
        Task<IReadOnlyList<CallRequestSummaryDto>> GetQueueAsync(
            OfficeCaller caller,
            CancellationToken cancellationToken = default);

        Task<CallRequestDetailDto?> GetDetailAsync(
            OfficeCaller caller,
            int id,
            CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> ClaimAsync(OfficeCaller caller, int id, CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> TakeOverAsync(OfficeCaller caller, int id, CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> ReleaseAsync(OfficeCaller caller, int id, CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> NoAnswerAsync(OfficeCaller caller, int id, CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> ResolveAsync(
            OfficeCaller caller,
            int id,
            ResolveCallRequestDto input,
            CancellationToken cancellationToken = default);

        Task<CallRequestResult<CallRequestSummaryDto>> ReopenAsync(OfficeCaller caller, int id, CancellationToken cancellationToken = default);
    }
}

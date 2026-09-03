using Raphael.Shared.DTOs.Realtime;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// What a dispatch screen can be told. Each method is one message the server may push.
    /// </summary>
    /// <remarks>
    /// This contract is the reason the hub is typed: the client method names live here, in one
    /// place, instead of as strings scattered through the services that publish.
    /// </remarks>
    public interface IDispatchClient
    {
        Task TripRouted(TripRoutedMessage message);

        Task TripUnrouted(TripUnroutedMessage message);

        Task RouteChanged(RouteChangedMessage message);

        Task VehiclePosition(VehiclePositionMessage message);
    }
}

using Raphael.Shared.DTOs.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Travel times, distances and coordinates — from our own cache when we already know them,
    /// from Google when we do not.
    /// </summary>
    /// <remarks>
    /// Every client in the ecosystem goes through here, and nothing else talks to Google. That is
    /// what makes one dispatcher's answer serve the next dispatcher and the driver, and it is what
    /// keeps the API key off the machines of people who can read it.
    /// </remarks>
    public interface IRoutingService
    {
        /// <summary>
        /// Prices a batch of legs. Answers come back in the order asked, one per leg, always.
        /// </summary>
        /// <remarks>
        /// A leg Google could not price comes back with status <c>Unavailable</c> rather than
        /// failing the batch: one bad pair of coordinates in a thirty-stop route must not leave a
        /// dispatcher with no route at all.
        /// </remarks>
        Task<RouteLegsResponseDto> GetLegsAsync(
            RouteLegsRequestDto request,
            CancellationToken cancellationToken);

        Task<GeocodeResultDto> GeocodeAsync(
            GeocodeRequestDto request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Resolves many addresses, paying only for the ones nobody has resolved before.
        /// Duplicates within the batch are resolved once.
        /// </summary>
        Task<GeocodeBatchResponseDto> GeocodeBatchAsync(
            GeocodeBatchRequestDto request,
            CancellationToken cancellationToken);

        /// <summary>
        /// What a place the client already looked up contains, if we have been told before.
        /// </summary>
        /// <remarks>
        /// Status <c>NotFound</c> is not a failure here: it means nobody has bought this place
        /// yet, and the caller — which holds the only key with Places enabled — should fetch it
        /// and hand it back through <see cref="StorePlaceAsync"/>.
        /// </remarks>
        Task<PlaceDetailsDto> GetPlaceAsync(string placeId, CancellationToken cancellationToken);

        /// <summary>Remembers a place a client had to buy, so nobody buys it twice.</summary>
        Task StorePlaceAsync(PlaceDetailsDto place, CancellationToken cancellationToken);

        Task<ReverseGeocodeResultDto> ReverseGeocodeCityAsync(
            ReverseGeocodeRequestDto request,
            CancellationToken cancellationToken);
    }
}

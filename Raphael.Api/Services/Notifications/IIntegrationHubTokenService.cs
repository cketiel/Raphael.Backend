namespace Raphael.Api.Services.Notifications
{
    /// <summary>
    /// Token an integration uses to open the notification hub, and when it stops working.
    /// </summary>
    /// <param name="AccessToken">Bearer token. Pass it to the hub as <c>access_token</c>.</param>
    /// <param name="ExpiresAtUtc">Moment the token stops being accepted.</param>
    public sealed record IntegrationHubToken(
        string AccessToken,
        DateTime ExpiresAtUtc);

    /// <inheritdoc cref="IntegrationHubTokenService"/>
    public interface IIntegrationHubTokenService
    {
        IntegrationHubToken Issue(int integratorId, string? integratorName);
    }
}

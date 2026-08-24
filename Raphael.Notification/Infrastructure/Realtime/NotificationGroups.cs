namespace Raphael.Notification.Infrastructure.Realtime;

/// <summary>
/// Naming of the SignalR groups used to deliver notifications in real time.
/// Kept in one place so the hub and the dispatcher can never drift apart.
/// </summary>
public static class NotificationGroups
{
    /// <summary>
    /// One group per patient, joined by the Raphael.Rider app.
    /// The name predates this class and must not change: existing clients rely on it.
    /// </summary>
    public static string Customer(int customerId)
        => $"Customer_{customerId}";

    /// <summary>
    /// One group per external system, joined by integrations that exchanged their
    /// API Key for a short lived token.
    /// </summary>
    public static string Integrator(int integratorId)
        => $"Integrator_{integratorId}";

    /// <summary>
    /// Every Raphael.Desktop user. Operational notices that the whole dispatch office
    /// must see are stored once and broadcast here, instead of one row per dispatcher.
    /// </summary>
    public const string DesktopAudience = "DesktopUsers";
}

namespace Raphael.Notification.Infrastructure.Realtime;

/// <summary>
/// Deployment level configuration of the notification hub.
/// Bound from the "Notifications:Realtime" section.
/// </summary>
public sealed class NotificationRealtimeOptions
{
    public const string SectionName = "Notifications:Realtime";

    /// <summary>
    /// Role identifiers that belong to drivers using Raphael.Driver.
    /// </summary>
    /// <remarks>
    /// Desktop users and drivers are both rows of the Users table, so the token alone
    /// does not say which application is connecting. The role decides, and the role
    /// comes from the token, never from the client: a driver must not be able to ask
    /// for a seat in the dispatch office broadcast.
    ///
    /// <para>
    /// While this list is empty every internal user is treated as a driver, which is the
    /// least privileged of the two. That keeps office notices private, at the cost of
    /// Raphael.Desktop receiving nothing until the deployment fills it in.
    /// </para>
    /// </remarks>
    public int[] DriverRoleIds { get; set; } = [];
}

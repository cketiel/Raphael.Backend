namespace Raphael.Notification.Application.Queries.GetRecipientNotifications;

/// <summary>
/// Which half of a recipient's rows to return.
/// </summary>
/// <remarks>
/// Notifications and signals live in the same tables but are meant for different readers: a
/// notice is for a person and belongs in an inbox, a signal is an instruction to an
/// application and would say nothing to anybody.
///
/// <para>
/// ⚠️ The split is enforced here, on the server, and not left to each client. A client that
/// forgot to filter would put rows nobody can act on in a driver's inbox and inflate a badge
/// that never clears.
/// </para>
/// </remarks>
public enum NotificationScope
{
    /// <summary>What a person reads. The default, and what the bell counts.</summary>
    Notices = 0,

    /// <summary>What the application acts on and then deletes.</summary>
    Signals = 1
}

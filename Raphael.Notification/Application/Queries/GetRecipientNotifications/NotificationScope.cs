namespace Raphael.Notification.Application.Queries.GetRecipientNotifications;

/// <summary>
/// Which of a recipient's rows to return.
/// </summary>
/// <remarks>
/// Notifications and signals live in the same tables but are written for different readers:
/// a notice is for a person, a signal is an instruction to an application.
///
/// <para>
/// ⚠️ The split is decided here, on the server, and not left to each client. Whichever way
/// it is set, every client sees the same thing: a rule that lives in one place cannot be
/// forgotten by the next application somebody writes.
/// </para>
///
/// <para>
/// The driver inbox asks for <see cref="All"/> today: the route signal is shown in the bell
/// like any other row while we decide what a driver should be told about their route
/// changing. Rider, Desktop and Integration ask for <see cref="Notices"/>, which is the
/// default, and no signal is addressed to them anyway.
/// </para>
/// </remarks>
public enum NotificationScope
{
    /// <summary>What a person reads. The default.</summary>
    Notices = 0,

    /// <summary>What an application acts on.</summary>
    Signals = 1,

    /// <summary>Both. What the driver inbox and the driver bell show today.</summary>
    All = 2
}

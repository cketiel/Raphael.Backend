namespace Raphael.Notification.Domain.Definitions;

public sealed class DeliveryChannel : NotificationEnumeration
{
    public static readonly DeliveryChannel InApp =
        new(
            1,
            "IN_APP",
            "In-App",
            "Displays the notification inside a Raphael application.",
            1,
            true,
            false,
            true);

    public static readonly DeliveryChannel Push =
        new(
            2,
            "PUSH",
            "Push Notification",
            "Sends a mobile push notification.",
            2,
            true,
            false,
            true);

    public static readonly DeliveryChannel Email =
        new(
            3,
            "EMAIL",
            "Email",
            "Sends the notification by email.",
            3,
            false,
            true,
            false);

    public static readonly DeliveryChannel Sms =
        new(
            4,
            "SMS",
            "SMS",
            "Sends the notification as a text message.",
            4,
            false,
            true,
            false);

    public static readonly DeliveryChannel Webhook =
        new(
            5,
            "WEBHOOK",
            "Webhook",
            "Delivers the notification to an external system.",
            5,
            false,
            true,
            false);

    public static readonly DeliveryChannel Broadcast =
        new(
            6,
            "BROADCAST",
            "Broadcast",
            "Publishes a system-wide announcement.",
            6,
            true,
            false,
            true);

    /// <summary>
    /// Indicates whether this channel provides real-time delivery.
    /// </summary>
    public bool IsRealTime { get; }

    /// <summary>
    /// Indicates whether this channel targets an external destination.
    /// </summary>
    public bool IsExternal { get; }

    /// <summary>
    /// Indicates whether this channel supports persistence
    /// in the Notification Center.
    /// </summary>
    public bool SupportsInbox { get; }

    private DeliveryChannel(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool isRealTime,
        bool isExternal,
        bool supportsInbox)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        IsRealTime = isRealTime;
        IsExternal = isExternal;
        SupportsInbox = supportsInbox;
    }
}
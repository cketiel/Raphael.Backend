namespace Raphael.Shared.Definitions.Notifications;

public sealed class NotificationStatus : NotificationEnumeration
{
    public static readonly NotificationStatus Created =
        new(
            1,
            "CREATED",
            "Created",
            "The notification has been created.",
            1,
            false,
            false,
            false);

    public static readonly NotificationStatus PendingDelivery =
        new(
            2,
            "PENDING_DELIVERY",
            "Pending Delivery",
            "Waiting to be delivered.",
            2,
            false,
            false,
            false);

    public static readonly NotificationStatus Delivered =
        new(
            3,
            "DELIVERED",
            "Delivered",
            "Successfully delivered to the recipient.",
            3,
            true,
            false,
            false);

    public static readonly NotificationStatus Viewed =
        new(
            4,
            "VIEWED",
            "Viewed",
            "The recipient has viewed the notification.",
            4,
            true,
            false,
            false);

    public static readonly NotificationStatus Acknowledged =
        new(
            5,
            "ACKNOWLEDGED",
            "Acknowledged",
            "The recipient explicitly acknowledged the notification.",
            5,
            true,
            true,
            false);

    public static readonly NotificationStatus Archived =
        new(
            6,
            "ARCHIVED",
            "Archived",
            "The notification is archived for historical purposes.",
            6,
            true,
            false,
            true);

    public static readonly NotificationStatus Expired =
        new(
            7,
            "EXPIRED",
            "Expired",
            "The notification expired before user interaction.",
            7,
            true,
            false,
            true);

    public static readonly NotificationStatus Cancelled =
        new(
            8,
            "CANCELLED",
            "Cancelled",
            "The notification was cancelled before delivery.",
            8,
            true,
            false,
            true);

    /// <summary>
    /// Indicates whether the notification has reached the recipient.
    /// </summary>
    public bool IsDelivered { get; }

    /// <summary>
    /// Indicates whether the user has explicitly acknowledged the notification.
    /// </summary>
    public bool IsAcknowledged { get; }

    /// <summary>
    /// Indicates whether this is a terminal state.
    /// </summary>
    public bool IsFinalState { get; }

    private NotificationStatus(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool isDelivered,
        bool isAcknowledged,
        bool isFinalState)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        IsDelivered = isDelivered;
        IsAcknowledged = isAcknowledged;
        IsFinalState = isFinalState;
    }
}
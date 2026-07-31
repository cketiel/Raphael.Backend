namespace Raphael.Notification.Domain.Models;

public class NotificationAction
{
    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    /// <summary>
    /// Unique action identifier interpreted by client applications.
    /// Example: VIEW_TRIP, RATE_TRIP
    /// </summary>
    public string ActionCode { get; private set; }

    /// <summary>
    /// Order in which actions should be displayed.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Indicates if this action is the primary action.
    /// </summary>
    public bool IsPrimary { get; private set; }


    private NotificationAction()
    {
        // Required by EF Core
    }


    public NotificationAction(
        Guid notificationId,
        string actionCode,
        int sortOrder,
        bool isPrimary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionCode);

        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Id = Guid.NewGuid();

        NotificationId = notificationId;

        ActionCode = actionCode;

        SortOrder = sortOrder;

        IsPrimary = isPrimary;
    }
}
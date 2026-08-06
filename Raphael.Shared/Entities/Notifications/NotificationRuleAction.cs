namespace Raphael.Shared.Entities.Notifications;

public class NotificationRuleAction
{
    public Guid Id { get; private set; }


    public Guid NotificationRuleId { get; private set; }


    public NotificationRule NotificationRule { get; private set; }


    /// <summary>
    /// Unique action identifier executed by the system.
    /// Example: CREATE_INCIDENT
    /// </summary>
    public string ActionCode { get; private set; }


    /// <summary>
    /// Optional parameter required by the action.
    /// Example: IncidentType=LateTrip
    /// </summary>
    public string? Parameters { get; private set; }


    /// <summary>
    /// Defines execution order when multiple actions exist.
    /// </summary>
    public int Order { get; private set; }


    private NotificationRuleAction()
    {
        // Required by EF Core
    }


    public NotificationRuleAction(
        NotificationRule notificationRule,
        string actionCode,
        string? parameters,
        int order = 1)
    {
        ArgumentNullException.ThrowIfNull(notificationRule);

        ArgumentException.ThrowIfNullOrWhiteSpace(actionCode);

        ArgumentOutOfRangeException.ThrowIfLessThan(order, 1);


        Id = Guid.NewGuid();

        NotificationRule = notificationRule;

        NotificationRuleId = notificationRule.Id;

        ActionCode = actionCode;

        Parameters = parameters;

        Order = order;
    }

    public void Update(
    string actionCode,
    string? parameters,
    int order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(order, 1);

        ActionCode = actionCode;
        Parameters = parameters;
        Order = order;
    }
}
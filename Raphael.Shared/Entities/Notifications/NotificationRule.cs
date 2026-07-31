using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationRule
{
    public Guid Id { get; private set; }


    public Guid BusinessEventDefinitionId { get; private set; }


    public BusinessEventDefinition BusinessEventDefinition { get; private set; }


    /// <summary>
    /// Unique rule identifier.
    /// Example: DRIVER_ROUTE_MODIFIED_NOTIFICATION
    /// </summary>
    public string Code { get; private set; }


    public string Name { get; private set; }


    public string Description { get; private set; }


    public NotificationType NotificationType { get; private set; }


    public NotificationPriority Priority { get; private set; }


    public NotificationSeverity Severity { get; private set; }


    public bool IsActive { get; private set; }

    public ICollection<NotificationRuleCondition> Conditions { get; private set; }


    public ICollection<NotificationRuleRecipient> Recipients { get; private set; }


    public ICollection<NotificationRuleChannel> Channels { get; private set; }


    public ICollection<NotificationRuleAction> Actions { get; private set; }


    private NotificationRule()
    {
        // Required by EF Core
    }


    public NotificationRule(
        BusinessEventDefinition businessEventDefinition,
        string code,
        string name,
        string description,
        NotificationType notificationType,
        NotificationPriority priority,
        NotificationSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(businessEventDefinition);

        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ArgumentNullException.ThrowIfNull(notificationType);

        ArgumentNullException.ThrowIfNull(priority);

        ArgumentNullException.ThrowIfNull(severity);


        Id = Guid.NewGuid();

        BusinessEventDefinition = businessEventDefinition;

        BusinessEventDefinitionId = businessEventDefinition.Id;

        Code = code;

        Name = name;

        Description = description;

        NotificationType = notificationType;

        Priority = priority;

        Severity = severity;

        IsActive = true;
        Conditions = new List<NotificationRuleCondition>();

        Recipients = new List<NotificationRuleRecipient>();

        Channels = new List<NotificationRuleChannel>();

        Actions = new List<NotificationRuleAction>();
    }

    public void AddCondition(
        NotificationRuleCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        Conditions.Add(condition);
    }



    public void AddRecipient(
        NotificationRuleRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        Recipients.Add(recipient);
    }



    public void AddChannel(
        NotificationRuleChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        Channels.Add(channel);
    }



    public void AddAction(
        NotificationRuleAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Actions.Add(action);
    }

    public void Disable()
    {
        IsActive = false;
    }


    public void Enable()
    {
        IsActive = true;
    }
}
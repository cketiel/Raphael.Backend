using Raphael.Notification.Domain.Definitions;

namespace Raphael.Notification.Domain.Rules;

public class NotificationRuleRecipient
{
    public Guid Id { get; private set; }


    public Guid NotificationRuleId { get; private set; }


    public NotificationRule NotificationRule { get; private set; }


    public RecipientType RecipientType { get; private set; }


    /// <summary>
    /// Defines the order in which recipients are processed.
    /// </summary>
    public int PriorityOrder { get; private set; }


    private NotificationRuleRecipient()
    {
        // Required by EF Core
    }


    public NotificationRuleRecipient(
        NotificationRule notificationRule,
        RecipientType recipientType,
        int priorityOrder = 1)
    {
        ArgumentNullException.ThrowIfNull(notificationRule);

        ArgumentNullException.ThrowIfNull(recipientType);

        ArgumentOutOfRangeException.ThrowIfLessThan(priorityOrder, 1);


        Id = Guid.NewGuid();

        NotificationRule = notificationRule;

        NotificationRuleId = notificationRule.Id;

        RecipientType = recipientType;

        PriorityOrder = priorityOrder;
    }
}
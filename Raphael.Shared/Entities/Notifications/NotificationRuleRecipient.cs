

using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationRuleRecipient
{
    public Guid Id { get; private set; }


    public Guid NotificationRuleId { get; private set; }


    public NotificationRule NotificationRule { get; private set; }

    public int RecipientTypeId { get; private set; }
    [NotMapped]
    public RecipientType RecipientType
    => Enumeration.FromId<RecipientType>(RecipientTypeId);
    //public RecipientType RecipientType { get; private set; }


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

        RecipientTypeId = recipientType.Id;
        //RecipientType = recipientType;

        PriorityOrder = priorityOrder;
    }

    public void Update(
    RecipientType recipientType,
    int priorityOrder)
    {
        ArgumentNullException.ThrowIfNull(recipientType);
        ArgumentOutOfRangeException.ThrowIfLessThan(priorityOrder, 1);

        RecipientTypeId = recipientType.Id;
        PriorityOrder = priorityOrder;
    }

}
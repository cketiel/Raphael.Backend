using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities.Notifications;

public class NotificationRuleChannel
{
    public Guid Id { get; private set; }


    public Guid NotificationRuleId { get; private set; }


    public NotificationRule NotificationRule { get; private set; }

    public int ChannelId { get; private set; }
    [NotMapped]
    public DeliveryChannel Channel
    => Enumeration.FromId<DeliveryChannel>(ChannelId);
    //public DeliveryChannel Channel { get; private set; }


    /// <summary>
    /// Defines the order in which channels are processed.
    /// </summary>
    public int PriorityOrder { get; private set; }


    /// <summary>
    /// Indicates if delivery through this channel is mandatory.
    /// </summary>
    public bool IsRequired { get; private set; }


    private NotificationRuleChannel()
    {
        // Required by EF Core
    }


    public NotificationRuleChannel(
        NotificationRule notificationRule,
        DeliveryChannel channel,
        int priorityOrder = 1,
        bool isRequired = false)
    {
        ArgumentNullException.ThrowIfNull(notificationRule);

        ArgumentNullException.ThrowIfNull(channel);

        ArgumentOutOfRangeException.ThrowIfLessThan(priorityOrder, 1);


        Id = Guid.NewGuid();

        NotificationRule = notificationRule;

        NotificationRuleId = notificationRule.Id;
        ChannelId = channel.Id;
        //Channel = channel;

        PriorityOrder = priorityOrder;

        IsRequired = isRequired;
    }

    public void Update(
    DeliveryChannel channel,
    int priorityOrder,
    bool isRequired)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentOutOfRangeException.ThrowIfLessThan(priorityOrder, 1);

        ChannelId = channel.Id;
        PriorityOrder = priorityOrder;
        IsRequired = isRequired;
    }

}
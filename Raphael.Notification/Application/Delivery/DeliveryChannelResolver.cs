using Raphael.Notification.Domain.Definitions;
using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Application.Delivery;


public class DeliveryChannelResolver
{
    public IEnumerable<DeliveryChannel> Resolve(
        IEnumerable<NotificationRuleChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);


        return channels
            .Select(x => x.Channel)
            .Distinct();
    }
}
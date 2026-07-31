using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Domain.Engine;

public class ChannelResolver
{
    public IEnumerable<NotificationRuleChannel> Resolve(
        NotificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);


        return rule.Channels;
    }
}
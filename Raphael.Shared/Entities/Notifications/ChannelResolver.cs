namespace Raphael.Shared.Entities.Notifications;

public class ChannelResolver
{
    public IEnumerable<NotificationRuleChannel> Resolve(
        NotificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);


        return rule.Channels;
    }
}
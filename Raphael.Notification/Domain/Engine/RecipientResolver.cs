using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Domain.Engine;

public class RecipientResolver
{
    public IEnumerable<NotificationRuleRecipient> Resolve(
        NotificationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);


        return rule.Recipients;
    }
}
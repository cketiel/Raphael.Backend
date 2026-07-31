using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Rules;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationRuleEvaluator
    : INotificationRuleEvaluator
{
    public Task<bool> EvaluateAsync(
        NotificationRule rule,
        BusinessEventContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ArgumentNullException.ThrowIfNull(context);


        // Fase inicial:
        // Si la regla no tiene condiciones,
        // significa que siempre aplica.

        if (rule.Conditions == null ||
            rule.Conditions.Count == 0)
        {
            return Task.FromResult(true);
        }


        foreach (var condition in rule.Conditions)
        {
            var result = EvaluateCondition(
                condition,
                context);

            if (!result)
            {
                return Task.FromResult(false);
            }
        }


        return Task.FromResult(true);
    }


    private static bool EvaluateCondition(
        NotificationRuleCondition condition,
        BusinessEventContext context)
    {
        if (!context.Data.TryGetValue(
                condition.Field,
                out var value))
        {
            return false;
        }


        return condition.Operator switch
        {
            "Equals" =>
                string.Equals(
                    value?.ToString(),
                    condition.Value,
                    StringComparison.OrdinalIgnoreCase),


            "NotEquals" =>
                !string.Equals(
                    value?.ToString(),
                    condition.Value,
                    StringComparison.OrdinalIgnoreCase),


            "Contains" =>
                value?.ToString()
                    ?.Contains(condition.Value,
                    StringComparison.OrdinalIgnoreCase)
                    == true,


            _ => false
        };
    }
}
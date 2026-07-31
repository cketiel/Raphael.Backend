using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Domain.Engine;

public class ConditionEvaluator
{
    public bool Evaluate(
        NotificationRuleCondition condition,
        Dictionary<string, string> data)
    {
        ArgumentNullException.ThrowIfNull(condition);

        ArgumentNullException.ThrowIfNull(data);


        if (!data.TryGetValue(
                condition.Field,
                out var actualValue))
        {
            return false;
        }


        return condition.Operator switch
        {
            "Equals" =>
                actualValue == condition.Value,


            "NotEquals" =>
                actualValue != condition.Value,


            "Contains" =>
                actualValue.Contains(condition.Value),


            "GreaterThan" =>
                Convert.ToDecimal(actualValue)
                >
                Convert.ToDecimal(condition.Value),


            "LessThan" =>
                Convert.ToDecimal(actualValue)
                <
                Convert.ToDecimal(condition.Value),


            _ => false
        };
    }
}
using Raphael.Notification.Domain.Events.Payloads;
using Raphael.Notification.Domain.Factories;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;
using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Domain.Engine;


public class NotificationRuleEngine
{
    private readonly ConditionEvaluator _conditionEvaluator;

    private readonly NotificationFactory _factory;


    public NotificationRuleEngine(
        ConditionEvaluator conditionEvaluator,
        NotificationFactory factory)
    {
        _conditionEvaluator = conditionEvaluator;

        _factory = factory;
    }



    public NotificationModel? Execute(
        NotificationRule rule,
        NotificationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ArgumentNullException.ThrowIfNull(payload);



        foreach (var condition in rule.Conditions)
        {
            var result =
                _conditionEvaluator.Evaluate(
                    condition,
                    payload.Data);


            if (!result)
            {
                return null;
            }
        }


        return _factory.Create(
            rule,
            payload);
    }
}
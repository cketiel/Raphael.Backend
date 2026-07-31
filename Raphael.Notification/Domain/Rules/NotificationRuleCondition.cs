namespace Raphael.Notification.Domain.Rules;

public class NotificationRuleCondition
{
    public Guid Id { get; private set; }


    public Guid NotificationRuleId { get; private set; }


    public NotificationRule NotificationRule { get; private set; }


    /// <summary>
    /// Field or property evaluated by the condition.
    /// Example: DelayMinutes
    /// </summary>
    public string Field { get; private set; }


    /// <summary>
    /// Operator used for comparison.
    /// Example: GreaterThan, Equals
    /// </summary>
    public string Operator { get; private set; }


    /// <summary>
    /// Expected value.
    /// Example: 10
    /// </summary>
    public string Value { get; private set; }


    /// <summary>
    /// Order used when multiple conditions exist.
    /// </summary>
    public int Order { get; private set; }


    private NotificationRuleCondition()
    {
        // Required by EF Core
    }


    public NotificationRuleCondition(
        NotificationRule notificationRule,
        string field,
        string operatorName,
        string value,
        int order = 1)
    {
        ArgumentNullException.ThrowIfNull(notificationRule);

        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        ArgumentException.ThrowIfNullOrWhiteSpace(operatorName);

        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ArgumentOutOfRangeException.ThrowIfLessThan(order, 1);


        Id = Guid.NewGuid();

        NotificationRule = notificationRule;

        NotificationRuleId = notificationRule.Id;

        Field = field;

        Operator = operatorName;

        Value = value;

        Order = order;
    }
}
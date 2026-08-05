namespace Raphael.Shared.Catalog.NotificationRules
{
    public sealed class NotificationRuleConditionCatalogItem
    {
        public string Field { get; init; } = string.Empty;

        public string Operator { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }
}

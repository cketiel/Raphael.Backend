using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Catalog.NotificationRules;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Services;

public sealed class NotificationRuleCatalogSeeder
{
    private readonly RaphaelContext _context;

    public NotificationRuleCatalogSeeder(RaphaelContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in NotificationRuleCatalog.Rules)
        {
            var definition = await _context.BusinessEventDefinitions
                .FirstOrDefaultAsync(x => x.Code == item.BusinessEventCode, cancellationToken);

            if (definition == null)
                throw new InvalidOperationException(
                    $"Business Event '{item.BusinessEventCode}' does not exist.");

            var existingRule = await _context.NotificationRules
                .FirstOrDefaultAsync(x => x.Code == item.RuleCode, cancellationToken);

            if (existingRule != null)
                continue;

            var rule = new NotificationRule(
                definition,
                item.RuleCode,
                item.RuleName,
                item.Description,
                item.Type,
                item.Priority,
                item.Severity);

            _context.NotificationRules.Add(rule);

            //------------------------------------------
            // Recipients
            //------------------------------------------

            int recipientOrder = 1;

            foreach (var recipient in item.Recipients)
            {
                rule.AddRecipient(
                    new NotificationRuleRecipient(
                        rule,
                        recipient,
                        recipientOrder++));
            }

            //------------------------------------------
            // Channels
            //------------------------------------------

            int channelOrder = 1;

            foreach (var channel in item.Channels)
            {
                rule.AddChannel(
                    new NotificationRuleChannel(
                        rule,
                        channel,
                        channelOrder++,
                        false));
            }

            //------------------------------------------
            // Actions
            //------------------------------------------

            int actionOrder = 1;

            foreach (var action in item.Actions)
            {
                rule.AddAction(
                    new NotificationRuleAction(
                        rule,
                        action,
                        null,
                        actionOrder++));
            }

            //------------------------------------------
            // Conditions
            //------------------------------------------

            int conditionOrder = 1;

            foreach (var condition in item.Conditions)
            {
                rule.AddCondition(
                    new NotificationRuleCondition(
                        rule,
                        condition.Field,
                        condition.Operator,
                        condition.Value,
                        conditionOrder++));
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
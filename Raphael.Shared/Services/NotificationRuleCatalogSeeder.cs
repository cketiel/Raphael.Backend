using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Catalog.NotificationRules;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;
using Raphael.Shared.Entities.Notifications;


/*Insertar solamente reglas nuevas
POST
/api/admin/notification/catalog/notification-rules
namespace Raphael.Shared.Services;
Sincronizar cambios del catálogo:
    POST
/api/admin/notification/catalog/notification-rules? updateExisting = true*/

public sealed class NotificationRuleCatalogSeeder
{
    private readonly RaphaelContext _context;

    public NotificationRuleCatalogSeeder(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task SeedAsync(
        bool updateExisting = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Channels)
            .Include(x => x.Recipients)
            .Include(x => x.Actions)
            .Include(x => x.Conditions)
            .ToDictionaryAsync(
                x => x.Code,
                cancellationToken);


        var definitions = await _context.BusinessEventDefinitions
            .ToDictionaryAsync(
                x => x.Code,
                cancellationToken);


        foreach (var item in NotificationRuleCatalog.Rules)
        {
            if (!definitions.TryGetValue(
                item.BusinessEventCode,
                out var businessEventDefinition))
            {
                throw new InvalidOperationException(
                    $"Business Event Definition '{item.BusinessEventCode}' was not found.");
            }


            // =====================================================
            // CREATE NEW RULE
            // =====================================================

            if (!rules.TryGetValue(item.RuleCode, out var rule))
            {
                rule = new NotificationRule(
                    businessEventDefinition,
                    item.RuleCode,
                    item.RuleName,
                    item.Description,
                    item.Type,
                    item.Priority,
                    item.Severity);


                _context.NotificationRules.Add(rule);


                SeedRecipients(rule, item);

                SeedChannels(rule, item);

                SeedActions(rule, item);


                rules.Add(rule.Code, rule);

                continue;
            }


            // =====================================================
            // UPDATE EXISTING RULE
            // =====================================================

            if (!updateExisting)
                continue;


            var notificationType =
                Enumeration.FromId<NotificationType>(
                    item.Type.Id);


            var priority =
                Enumeration.FromId<NotificationPriority>(
                    item.Priority.Id);


            var severity =
                Enumeration.FromId<NotificationSeverity>(
                    item.Severity.Id);


            rule.UpdateConfiguration(
                notificationType,
                priority,
                severity);


            rule.SetActive(true);


            UpdateRecipients(rule, item);

            UpdateChannels(rule, item);

            UpdateActions(rule, item);
        }


        await _context.SaveChangesAsync(cancellationToken);
    }



    private void SeedRecipients(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        foreach (var recipientType in item.Recipients)
        {
            var recipient =
                new NotificationRuleRecipient(
                    rule,
                    recipientType);


            rule.AddRecipient(recipient);

            _context.NotificationRuleRecipients.Add(recipient);
        }
    }



    private void SeedChannels(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        var order = 1;

        foreach (var channel in item.Channels)
        {
            var ruleChannel =
                new NotificationRuleChannel(
                    rule,
                    channel,
                    order);


            rule.AddChannel(ruleChannel);

            _context.NotificationRuleChannels.Add(ruleChannel);

            order++;
        }
    }



    private void SeedActions(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        var order = 1;

        foreach (var actionCode in item.Actions)
        {
            var action =
                new NotificationRuleAction(
                    rule,
                    actionCode,
                    null,
                    order);


            rule.AddAction(action);

            _context.NotificationRuleActions.Add(action);

            order++;
        }
    }



    private void UpdateRecipients(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        _context.NotificationRuleRecipients.RemoveRange(
            rule.Recipients);


        rule.Recipients.Clear();


        SeedRecipients(rule, item);
    }



    private void UpdateChannels(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        _context.NotificationRuleChannels.RemoveRange(
            rule.Channels);


        rule.Channels.Clear();


        SeedChannels(rule, item);
    }



    private void UpdateActions(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        _context.NotificationRuleActions.RemoveRange(
            rule.Actions);


        rule.Actions.Clear();


        SeedActions(rule, item);
    }
}



/*using Microsoft.EntityFrameworkCore;
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
}*/
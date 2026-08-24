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


        // Checked up front and all at once. A rule pointing at an event that does not
        // exist aborts the whole synchronisation, and reporting only the first one turns
        // fixing the catalog into one deploy per missing event.
        var missing = NotificationRuleCatalog.Rules
            .Select(x => x.BusinessEventCode)
            .Where(code => !definitions.ContainsKey(code))
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Business Event Definitions not found: " +
                string.Join(", ", missing) +
                ". Run the business event catalog synchronization first.");
        }


        foreach (var item in NotificationRuleCatalog.Rules)
        {
            var businessEventDefinition = definitions[item.BusinessEventCode];


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

                SeedConditions(rule, item);


                rule.SetActive(item.Enabled);


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


            // The catalog decides whether a rule is on. This used to force every rule
            // active on each synchronisation, which silently switched back on anything
            // an administrator had turned off, and left rules alive for events nobody
            // publishes: those write notifications with no recipient at all.
            rule.SetActive(item.Enabled);


            UpdateRecipients(rule, item);

            UpdateChannels(rule, item);

            UpdateActions(rule, item);

            UpdateConditions(rule, item);
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



    /// <summary>
    /// Conditions were declared in the catalog but never written to the database, so a
    /// rule that depended on one applied unconditionally. The wired events express their
    /// audience through the payload instead, but the mechanism has to work for the ones
    /// that will need it.
    /// </summary>
    private void SeedConditions(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        var order = 1;

        foreach (var condition in item.Conditions)
        {
            var ruleCondition =
                new NotificationRuleCondition(
                    rule,
                    condition.Field,
                    condition.Operator,
                    condition.Value,
                    order);


            rule.AddCondition(ruleCondition);

            _context.NotificationRuleConditions.Add(ruleCondition);

            order++;
        }
    }



    private void UpdateConditions(
        NotificationRule rule,
        NotificationRuleCatalogItem item)
    {
        _context.NotificationRuleConditions.RemoveRange(
            rule.Conditions);


        rule.Conditions.Clear();


        SeedConditions(rule, item);
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
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Catalog.BusinessEvents;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Services;

public sealed class BusinessEventCatalogSeeder
{
    private readonly RaphaelContext _context;

    public BusinessEventCatalogSeeder(RaphaelContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _context.BusinessEventCategories
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var groups = await _context.BusinessEventGroups
            .Include(x => x.Category)
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var events = await _context.BusinessEvents
            .Include(x => x.Group)
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        var definitions = await _context.BusinessEventDefinitions
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var item in BusinessEventCatalog.Events)
        {
            // -----------------------------
            // Category
            // -----------------------------

            if (!categories.TryGetValue(item.CategoryCode, out var category))
            {
                category = new BusinessEventCategory(
                    item.CategoryCode,
                    item.CategoryName,
                    item.CategoryDescription);

                _context.BusinessEventCategories.Add(category);

                categories.Add(category.Code, category);
            }

            // -----------------------------
            // Group
            // -----------------------------

            if (!groups.TryGetValue(item.GroupCode, out var group))
            {
                group = new BusinessEventGroup(
                    category,
                    item.GroupCode,
                    item.GroupName,
                    item.GroupDescription);

                _context.BusinessEventGroups.Add(group);

                groups.Add(group.Code, group);
            }

            // -----------------------------
            // Business Event
            // -----------------------------

            if (!events.TryGetValue(item.EventCode, out var businessEvent))
            {
                businessEvent = new BusinessEvent(
                    item.EventCode,
                    item.EventName,
                    item.EventDescription,
                    group,
                    item.Source);

                _context.BusinessEvents.Add(businessEvent);

                events.Add(businessEvent.Code, businessEvent);
            }

            // -----------------------------
            // Business Event Definition
            // -----------------------------

            if (!definitions.ContainsKey(item.EventCode))
            {
                var definition = new BusinessEventDefinition(
                    businessEvent,
                    item.EventCode,
                    item.EventName,
                    //item.DisplayName,
                    item.EventDescription,
                    true);
                    //item.GeneratesNotification);

                _context.BusinessEventDefinitions.Add(definition);

                definitions.Add(definition.Code, definition);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
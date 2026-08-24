using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Catalog.BusinessEvents;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Services;

/// <summary>
/// Brings the business event catalog in the database in line with the one in code.
/// </summary>
/// <remarks>
/// This used to insert only, which is why the endpoint that ran it was commented out:
/// there was no safe way to re-run it, and a rule whose event is missing throws when the
/// catalog is synchronised. It now upserts, so it can be run whenever the catalog changes.
/// The event code is the identity and is never rewritten.
/// </remarks>
public sealed class BusinessEventCatalogSeeder
{
    private readonly RaphaelContext _context;

    public BusinessEventCatalogSeeder(RaphaelContext context)
    {
        _context = context;
    }

    /// <param name="updateExisting">
    /// False inserts what is missing and leaves the rest untouched. True also refreshes
    /// the names and descriptions of what is already there.
    /// </param>
    public async Task SeedAsync(
        bool updateExisting = false,
        CancellationToken cancellationToken = default)
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
            else if (updateExisting)
            {
                businessEvent.Update(
                    item.EventName,
                    item.EventDescription,
                    item.Source);
            }

            // -----------------------------
            // Business Event Definition
            // -----------------------------

            if (!definitions.TryGetValue(item.EventCode, out var definition))
            {
                definition = new BusinessEventDefinition(
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
            else if (updateExisting)
            {
                definition.Update(
                    item.EventName,
                    item.EventDescription,
                    true);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Catalog.BusinessEvents;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Services;

public class BusinessEventCatalogSeeder
{
    private readonly RaphaelContext _context;


    public BusinessEventCatalogSeeder(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task SeedAsync()
    {
        foreach (var item in BusinessEventCatalog.Events)
        {
            var category = await _context.BusinessEventCategories
                .FirstOrDefaultAsync(x => x.Code == item.CategoryCode);


            if (category == null)
            {
                category = new BusinessEventCategory(
                    item.CategoryCode,
                    item.CategoryName,
                    item.CategoryDescription);

                _context.BusinessEventCategories.Add(category);

                await _context.SaveChangesAsync();
            }



            var group = await _context.BusinessEventGroups
                .FirstOrDefaultAsync(x => x.Code == item.GroupCode);


            if (group == null)
            {
                group = new BusinessEventGroup(
                    category,
                    item.GroupCode,
                    item.GroupName,
                    item.GroupDescription);

                _context.BusinessEventGroups.Add(group);

                await _context.SaveChangesAsync();
            }



            var businessEvent = await _context.BusinessEvents
                .FirstOrDefaultAsync(x => x.Code == item.EventCode);


            if (businessEvent == null)
            {
                businessEvent = new BusinessEvent(
                    item.EventCode,
                    item.EventName,
                    item.EventDescription,
                    group,
                    item.Source);

                _context.BusinessEvents.Add(businessEvent);

                await _context.SaveChangesAsync();
            }
        }
    }
}
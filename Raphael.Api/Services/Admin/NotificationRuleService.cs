using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Mappers;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Domain.Common;

namespace Raphael.Api.Services.Admin;

public sealed class NotificationRuleService
{
    private readonly RaphaelContext _context;

    public NotificationRuleService(RaphaelContext context)
    {
        _context = context;
    }


    public async Task<List<NotificationRuleDto>> GetAllAsync()
    {
        var rules = await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .Include(x => x.Conditions)
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync();


        return rules
            .Select(NotificationRuleMapper.ToDto)
            .ToList();
    }



    public async Task<NotificationRuleDto?> GetByIdAsync(Guid id)
    {
        var rule = await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .Include(x => x.Conditions)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);


        if (rule == null)
            return null;


        return NotificationRuleMapper.ToDto(rule);
    }



    public async Task UpdateAsync(UpdateNotificationRuleDto dto)
    {
        var rule = await _context.NotificationRules
            .FirstOrDefaultAsync(x => x.Id == dto.Id);


        if (rule == null)
        {
            throw new InvalidOperationException(
                "Notification Rule not found.");
        }


        var notificationType =
            Enumeration.FromId<NotificationType>(
                dto.NotificationTypeId);


        var priority =
            Enumeration.FromId<NotificationPriority>(
                dto.PriorityId);


        var severity =
            Enumeration.FromId<NotificationSeverity>(
                dto.SeverityId);



        rule.UpdateConfiguration(
            notificationType,
            priority,
            severity);



        rule.SetActive(dto.IsActive);



        await _context.SaveChangesAsync();
    }



    public async Task<bool> SetActiveAsync(
        Guid id,
        bool isActive)
    {
        var rule = await _context.NotificationRules
            .FirstOrDefaultAsync(x => x.Id == id);


        if (rule == null)
            return false;


        rule.SetActive(isActive);


        await _context.SaveChangesAsync();


        return true;
    }



    /// <summary>
    /// Switches every rule of a business event group on or off at once.
    /// </summary>
    /// <remarks>
    /// Notifications are silenced by family, not one by one. If the cancellation notices
    /// turn out to be wrong or too noisy, somebody has to be able to stop all of them in
    /// one action, at three in the morning, without picking rules off a list.
    /// </remarks>
    /// <returns>How many rules changed.</returns>
    public async Task<int> SetGroupActiveAsync(
        string groupCode,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
                .ThenInclude(x => x.BusinessEvent)
                    .ThenInclude(x => x.Group)
            .Where(x => x.BusinessEventDefinition.BusinessEvent.Group.Code == groupCode)
            .ToListAsync(cancellationToken);


        foreach (var rule in rules)
        {
            rule.SetActive(isActive);
        }


        await _context.SaveChangesAsync(cancellationToken);


        return rules.Count;
    }



    /// <summary>
    /// Switches every rule of one business event on or off, whatever its audience.
    /// </summary>
    /// <returns>How many rules changed.</returns>
    public async Task<int> SetEventActiveAsync(
        string businessEventCode,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Where(x => x.BusinessEventDefinition.Code == businessEventCode)
            .ToListAsync(cancellationToken);


        foreach (var rule in rules)
        {
            rule.SetActive(isActive);
        }


        await _context.SaveChangesAsync(cancellationToken);


        return rules.Count;
    }



    public async Task<bool> UpdateGeneratesNotificationAsync(
        Guid businessEventDefinitionId,
        bool generatesNotification)
    {
        var definition = await _context.BusinessEventDefinitions
            .FirstOrDefaultAsync(
                x => x.Id == businessEventDefinitionId);


        if (definition == null)
            return false;



        if (generatesNotification)
        {
            definition.Enable();
        }
        else
        {
            definition.Disable();
        }



        await _context.SaveChangesAsync();


        return true;
    }
}
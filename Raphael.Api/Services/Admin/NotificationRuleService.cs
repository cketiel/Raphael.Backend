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
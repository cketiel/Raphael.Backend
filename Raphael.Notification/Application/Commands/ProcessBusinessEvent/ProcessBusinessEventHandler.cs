using Raphael.Notification.Application.Interfaces.Persistence;

namespace Raphael.Notification.Application.Commands.ProcessBusinessEvent;

public class ProcessBusinessEventHandler
{
    private readonly INotificationRuleRepository _notificationRuleRepository;


    public ProcessBusinessEventHandler(
        INotificationRuleRepository notificationRuleRepository)
    {
        _notificationRuleRepository = notificationRuleRepository;
    }


    public async Task Handle(
        ProcessBusinessEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var rule =
            await _notificationRuleRepository.GetActiveRuleAsync(
                command.BusinessEventCode,
                cancellationToken);


        if (rule == null)
        {
            return;
        }


        // Rule Engine will be executed here.
        //
        // 1. Evaluate Conditions
        // 2. Resolve Recipients
        // 3. Resolve Channels
        // 4. Resolve Priority
        // 5. Resolve Severity
        // 6. Resolve Type
        //
        // Then create Notification.
    }
}
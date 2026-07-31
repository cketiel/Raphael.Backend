using FluentValidation;
using Raphael.Notification.Application.Commands.ProcessBusinessEvent;

namespace Raphael.Notification.Application.Validators;

public class ProcessBusinessEventValidator
    : AbstractValidator<ProcessBusinessEventCommand>
{
    public ProcessBusinessEventValidator()
    {
        RuleFor(x => x.BusinessEventCode)
            .NotEmpty()
            .MaximumLength(100);


        RuleFor(x => x.EntityId)
            .NotEmpty();


        RuleFor(x => x.EntityType)
            .NotEmpty();


        RuleFor(x => x.Data)
            .NotNull();
    }
}
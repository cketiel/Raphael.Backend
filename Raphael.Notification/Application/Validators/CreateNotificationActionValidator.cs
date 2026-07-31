using FluentValidation;
using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Application.Validators;

public class CreateNotificationActionValidator
    : AbstractValidator<CreateNotificationActionRequest>
{
    public CreateNotificationActionValidator()
    {
        RuleFor(x => x.ActionCode)
            .NotEmpty()
            .MaximumLength(100);


        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
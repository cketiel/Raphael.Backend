using FluentValidation;
using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Application.Validators;

public class CreateNotificationRecipientValidator
    : AbstractValidator<CreateNotificationRecipientRequest>
{
    public CreateNotificationRecipientValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty();


        RuleFor(x => x.RecipientType)
            .NotEmpty();
    }
}
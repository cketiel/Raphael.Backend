using FluentValidation;
using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Application.Validators;

public class CreateNotificationValidator
    : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.BusinessEventCode)
            .NotEmpty()
            .MaximumLength(100);


        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);


        RuleFor(x => x.Message)
            .NotEmpty();


        RuleFor(x => x.Priority)
            .NotEmpty();


        RuleFor(x => x.Severity)
            .NotEmpty();


        RuleFor(x => x.Type)
            .NotEmpty();


        RuleForEach(x => x.Recipients)
            .SetValidator(
                new CreateNotificationRecipientValidator());


        RuleForEach(x => x.Actions)
            .SetValidator(
                new CreateNotificationActionValidator());
    }
}
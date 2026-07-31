using Raphael.Notification.Application.DTOs;

namespace Raphael.Notification.Application.Commands.CreateNotification;

public class CreateNotificationCommand
{
    public CreateNotificationRequest Request { get; }


    public CreateNotificationCommand(
        CreateNotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }
}
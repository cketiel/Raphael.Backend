using Raphael.Notification.Domain.Factories;
using Raphael.Shared.Entities.Notifications.Payloads;

namespace Raphael.Notification.Domain.Processing;


public class NotificationProcessor
{
    private readonly NotificationFactory _factory;


    public NotificationProcessor(
        NotificationFactory factory)
    {
        _factory = factory;
    }


    public NotificationProcessingResult Process(
        NotificationEventPayload payload,
        NotificationContext context)
    {
        ArgumentNullException.ThrowIfNull(payload);

        ArgumentNullException.ThrowIfNull(context);


        /*
         * En esta fase solamente dejamos
         * preparado el pipeline.
         *
         * La búsqueda de reglas y evaluación
         * llegará en la siguiente fase.
         */


        return NotificationProcessingResult.Ok(
            "Notification event processed");
    }
}
using Raphael.Notification.Domain.Models;
using Raphael.Notification.Domain.Rules;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;

namespace Raphael.Notification.Application.Delivery;


public class NotificationDeliveryService
{
    private readonly DeliveryChannelResolver _channelResolver;

    private readonly NotificationSenderFactory _senderFactory;



    public NotificationDeliveryService(
        DeliveryChannelResolver channelResolver,
        NotificationSenderFactory senderFactory)
    {
        _channelResolver = channelResolver;

        _senderFactory = senderFactory;
    }



    public async Task<List<NotificationSenderResult>> DeliverAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        IEnumerable<NotificationRuleChannel> channels,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationSenderResult>();


        var deliveryChannels =
            _channelResolver.Resolve(channels);



        foreach (var channel in deliveryChannels)
        {
            var sender =
                _senderFactory.Create(channel);



            var result =
                await sender.SendAsync(
                    notification,
                    recipient,
                    cancellationToken);



            results.Add(result);
        }


        return results;
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Models;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationDeliveryConfiguration :
    IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(
        EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries");


        builder.HasKey(x => x.Id);


        builder.Property(x => x.Channel)
            .IsRequired();


        builder.Property(x => x.Status)
            .IsRequired();


        builder.Property(x => x.ExternalMessageId)
            .HasMaxLength(200);


        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);



        builder.HasIndex(x => x.NotificationId);


        builder.HasIndex(x => x.Status);


        builder.HasIndex(x => x.Channel);


        builder.HasIndex(x => x.DeliveredAtUtc);
    }
}
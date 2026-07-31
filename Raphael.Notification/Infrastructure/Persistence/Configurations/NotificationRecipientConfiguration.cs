using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Models;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationRecipientConfiguration :
    IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(
        EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipients");


        builder.HasKey(x => x.Id);



        builder.Property(x => x.RecipientId)
            .IsRequired();


        builder.Property(x => x.RecipientType)
            .IsRequired();


        builder.Property(x => x.Status)
            .IsRequired();



        builder.Property(x => x.DeliveredAtUtc);


        builder.Property(x => x.ViewedAtUtc);


        builder.Property(x => x.AcknowledgedAtUtc);



        /*
         * Indexes
         */

        builder.HasIndex(x => x.NotificationId);


        builder.HasIndex(x => x.RecipientId);


        builder.HasIndex(x => x.RecipientType);



        builder.HasIndex(x => new
        {
            x.RecipientId,
            x.Status
        });



        builder.HasIndex(x => new
        {
            x.NotificationId,
            x.RecipientId
        })
        .IsUnique();
    }
}
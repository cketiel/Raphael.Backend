using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Models;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationActionConfiguration :
    IEntityTypeConfiguration<NotificationAction>
{
    public void Configure(
        EntityTypeBuilder<NotificationAction> builder)
    {
        builder.ToTable("NotificationActions");


        builder.HasKey(x => x.Id);


        builder.Property(x => x.ActionCode)
            .HasMaxLength(100)
            .IsRequired();


        builder.Property(x => x.SortOrder)
            .IsRequired();


        builder.Property(x => x.IsPrimary)
            .IsRequired();



        builder.HasIndex(x => x.NotificationId);


        builder.HasIndex(x => x.ActionCode);


        builder.HasIndex(x => new
        {
            x.NotificationId,
            x.SortOrder
        });


        builder.HasIndex(x => new
        {
            x.NotificationId,
            x.IsPrimary
        });
    }
}
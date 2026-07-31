using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationModel = Raphael.Notification.Domain.Models.Notification;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration :
    IEntityTypeConfiguration<NotificationModel>
{
    public void Configure(
        EntityTypeBuilder<NotificationModel> builder)
    {
        builder.ToTable("Notifications");


        builder.HasKey(x => x.Id);



        builder.Property(x => x.BusinessEventCode)
            .HasMaxLength(150)
            .IsRequired();



        builder.Property(x => x.Title)
            .HasMaxLength(250)
            .IsRequired();



        builder.Property(x => x.Message)
            .HasMaxLength(2000)
            .IsRequired();



        builder.Property(x => x.Priority)
            .IsRequired();



        builder.Property(x => x.Severity)
            .IsRequired();



        builder.Property(x => x.Type)
            .IsRequired();



        builder.Property(x => x.Status)
            .IsRequired();



        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();



        /*
         * Indexes
         */

        builder.HasIndex(x => x.BusinessEventCode);


        builder.HasIndex(x => x.Status);


        builder.HasIndex(x => x.CreatedAtUtc);



        builder.HasIndex(x => new
        {
            x.BusinessEventCode,
            x.Status
        });
    }
}
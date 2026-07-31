using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Models;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationMetadataConfiguration :
    IEntityTypeConfiguration<NotificationMetadata>
{
    public void Configure(
        EntityTypeBuilder<NotificationMetadata> builder)
    {
        builder.ToTable("NotificationMetadata");


        builder.HasKey(x => x.Id);



        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();



        builder.Property(x => x.Value)
            .HasMaxLength(1000)
            .IsRequired();



        /*
         * Indexes
         */


        builder.HasIndex(x => x.NotificationId);



        builder.HasIndex(x => new
        {
            x.NotificationId,
            x.Key
        })
        .IsUnique();
    }
}
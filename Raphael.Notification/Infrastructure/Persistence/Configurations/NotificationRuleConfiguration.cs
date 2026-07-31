using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationRuleConfiguration :
    IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(
        EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("NotificationRules");


        builder.HasKey(x => x.Id);



        builder.Property(x => x.Code)
            .HasMaxLength(150)
            .IsRequired();


        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();


        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();



        builder.Property(x => x.NotificationType)
            .IsRequired();


        builder.Property(x => x.Priority)
            .IsRequired();


        builder.Property(x => x.Severity)
            .IsRequired();


        builder.Property(x => x.IsActive)
            .IsRequired();



        /*
         * Relationship with BusinessEventDefinition
         */
        builder.HasOne(x => x.BusinessEventDefinition)
            .WithMany()
            .HasForeignKey(x => x.BusinessEventDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);



        /*
         * Indexes
         */

        builder.HasIndex(x => x.Code)
            .IsUnique();


        builder.HasIndex(x => x.BusinessEventDefinitionId);


        builder.HasIndex(x => x.IsActive);


        builder.HasIndex(x => new
        {
            x.BusinessEventDefinitionId,
            x.IsActive
        });
    }
}
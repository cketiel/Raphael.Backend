using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationRuleRecipientConfiguration :
    IEntityTypeConfiguration<NotificationRuleRecipient>
{
    public void Configure(
        EntityTypeBuilder<NotificationRuleRecipient> builder)
    {
        builder.ToTable("NotificationRuleRecipients");


        builder.HasKey(x => x.Id);



        /*
         * Relationship
         */

        builder.HasOne(x => x.NotificationRule)
            .WithMany(x => x.Recipients)
            .HasForeignKey(x => x.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);



        /*
         * Properties
         */

        builder.Property(x => x.RecipientType)
            .IsRequired();


        builder.Property(x => x.PriorityOrder)
            .IsRequired();



        /*
         * Indexes
         */

        builder.HasIndex(x => x.NotificationRuleId);


        builder.HasIndex(x => new
        {
            x.NotificationRuleId,
            x.PriorityOrder
        });


        builder.HasIndex(x => new
        {
            x.NotificationRuleId,
            x.RecipientType
        })
        .IsUnique();
    }
}
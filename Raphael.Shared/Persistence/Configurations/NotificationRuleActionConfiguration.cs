using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Persistence.Configurations;

public class NotificationRuleActionConfiguration :
    IEntityTypeConfiguration<NotificationRuleAction>
{
    public void Configure(
        EntityTypeBuilder<NotificationRuleAction> builder)
    {
        builder.ToTable("NotificationRuleActions");


        builder.HasKey(x => x.Id);



        /*
         * Relationship
         */

        builder.HasOne(x => x.NotificationRule)
            .WithMany(x => x.Actions)
            .HasForeignKey(x => x.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);



        /*
         * Properties
         */

        builder.Property(x => x.ActionCode)
            .HasMaxLength(150)
            .IsRequired();


        builder.Property(x => x.Parameters)
            .HasMaxLength(1000);



        builder.Property(x => x.Order)
            .IsRequired();



        /*
         * Indexes
         */

        builder.HasIndex(x => x.NotificationRuleId);



        builder.HasIndex(x => new
        {
            x.NotificationRuleId,
            x.Order
        });



        builder.HasIndex(x => new
        {
            x.NotificationRuleId,
            x.ActionCode
        })
        .IsUnique();
    }
}
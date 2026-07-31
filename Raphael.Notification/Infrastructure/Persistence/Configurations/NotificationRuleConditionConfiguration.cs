using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Notification.Domain.Rules;

namespace Raphael.Notification.Infrastructure.Persistence.Configurations;

public class NotificationRuleConditionConfiguration :
    IEntityTypeConfiguration<NotificationRuleCondition>
{
    public void Configure(
        EntityTypeBuilder<NotificationRuleCondition> builder)
    {
        builder.ToTable("NotificationRuleConditions");


        builder.HasKey(x => x.Id);



        /*
         * Relationship
         */

        builder.HasOne(x => x.NotificationRule)
            .WithMany(x => x.Conditions)
            .HasForeignKey(x => x.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);



        /*
         * Properties
         */

        builder.Property(x => x.Field)
            .HasMaxLength(100)
            .IsRequired();



        builder.Property(x => x.Operator)
            .HasMaxLength(50)
            .IsRequired();



        builder.Property(x => x.Value)
            .HasMaxLength(500)
            .IsRequired();



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
    }
}
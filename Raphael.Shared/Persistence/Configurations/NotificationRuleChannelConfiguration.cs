using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Shared.Persistence.Configurations;

public class NotificationRuleChannelConfiguration :
    IEntityTypeConfiguration<NotificationRuleChannel>
{
    public void Configure(
        EntityTypeBuilder<NotificationRuleChannel> builder)
    {
        builder.ToTable("NotificationRuleChannels");


        builder.HasKey(x => x.Id);



        /*
         * Relationship
         */

        builder.HasOne(x => x.NotificationRule)
            .WithMany(x => x.Channels)
            .HasForeignKey(x => x.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);



        /*
         * Properties
         */

        builder.Property(x => x.Channel)
            .IsRequired();


        builder.Property(x => x.PriorityOrder)
            .IsRequired();


        builder.Property(x => x.IsRequired)
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
            x.Channel
        })
        .IsUnique();
    }
}
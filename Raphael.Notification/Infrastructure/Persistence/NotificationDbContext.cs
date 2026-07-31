using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Infrastructure.Persistence;


public class NotificationDbContext : DbContext
{
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }


    public DbSet<NotificationModel> Notifications
        => Set<NotificationModel>();


    public DbSet<NotificationRecipient> NotificationRecipients
        => Set<NotificationRecipient>();


    public DbSet<NotificationDelivery> NotificationDeliveries
        => Set<NotificationDelivery>();


    public DbSet<NotificationMetadata> NotificationMetadata
        => Set<NotificationMetadata>();


    public DbSet<NotificationAction> NotificationActions
        => Set<NotificationAction>();


    public DbSet<NotificationRule> NotificationRules
        => Set<NotificationRule>();



    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NotificationDbContext).Assembly);
    }
}
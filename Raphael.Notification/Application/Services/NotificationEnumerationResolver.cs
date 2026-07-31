using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;
using System.Reflection;

namespace Raphael.Notification.Application.Services;

public class NotificationEnumerationResolver
{
    public NotificationPriority ResolvePriority(
        string value)
    {
        return GetValues<NotificationPriority>()
            .First(x =>
                x.Name.Equals(
                    value,
                    StringComparison.OrdinalIgnoreCase));
    }


    public NotificationSeverity ResolveSeverity(
        string value)
    {
        return GetValues<NotificationSeverity>()
            .First(x =>
                x.Name.Equals(
                    value,
                    StringComparison.OrdinalIgnoreCase));
    }


    public NotificationType ResolveType(
        string value)
    {
        return GetValues<NotificationType>()
            .First(x =>
                x.Name.Equals(
                    value,
                    StringComparison.OrdinalIgnoreCase));
    }


    private static IEnumerable<T> GetValues<T>()
        where T : class
    {
        return typeof(T)
            .GetFields(
                BindingFlags.Public |
                BindingFlags.Static)
            .Where(f =>
                f.FieldType == typeof(T))
            .Select(f =>
                (T)f.GetValue(null)!);
    }
}
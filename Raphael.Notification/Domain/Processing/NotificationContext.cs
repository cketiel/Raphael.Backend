namespace Raphael.Notification.Domain.Processing;

public class NotificationContext
{
    public string Application { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public string Culture { get; private set; }

    public string TimeZone { get; private set; }


    private NotificationContext()
    {
    }


    public NotificationContext(
        string application,
        Guid? userId,
        Guid? tenantId,
        string culture,
        string timeZone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(application);

        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);


        Application = application;

        UserId = userId;

        TenantId = tenantId;

        Culture = culture;

        TimeZone = timeZone;
    }
}
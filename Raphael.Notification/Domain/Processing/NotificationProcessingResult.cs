namespace Raphael.Notification.Domain.Processing;

public class NotificationProcessingResult
{
    public bool Success { get; private set; }


    public string Message { get; private set; }


    public List<Guid> NotificationIds { get; private set; }


    private NotificationProcessingResult()
    {
        NotificationIds = new();
    }


    private NotificationProcessingResult(
        bool success,
        string message)
    {
        Success = success;

        Message = message;

        NotificationIds = new();
    }


    public static NotificationProcessingResult Ok(
        string message)
    {
        return new NotificationProcessingResult(
            true,
            message);
    }


    public static NotificationProcessingResult Fail(
        string message)
    {
        return new NotificationProcessingResult(
            false,
            message);
    }


    public void AddNotification(Guid id)
    {
        NotificationIds.Add(id);
    }
}
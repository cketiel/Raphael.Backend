namespace Raphael.Notification.Application.Delivery;

public class NotificationSenderResult
{
    public bool Success { get; private set; }


    public string Message { get; private set; }


    public DateTime ProcessedAtUtc { get; private set; }



    private NotificationSenderResult(
        bool success,
        string message)
    {
        Success = success;

        Message = message;

        ProcessedAtUtc = DateTime.UtcNow;
    }



    public static NotificationSenderResult Ok(
        string message)
    {
        return new NotificationSenderResult(
            true,
            message);
    }



    public static NotificationSenderResult Fail(
        string message)
    {
        return new NotificationSenderResult(
            false,
            message);
    }
}
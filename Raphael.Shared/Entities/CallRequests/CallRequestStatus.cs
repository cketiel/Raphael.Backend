namespace Raphael.Shared.Entities.CallRequests
{
    /// <summary>Where a driver's request to be called back stands.</summary>
    public enum CallRequestStatus
    {
        Waiting = 0,
        InProgress = 1,
        Resolved = 2,
        Cancelled = 3,
        Expired = 4
    }

    /// <summary>One line of a request's timeline.</summary>
    public enum CallRequestEventType
    {
        Requested = 0,
        Reminded = 1,
        Claimed = 2,
        TakenOver = 3,
        Released = 4,
        CallNotAnswered = 5,
        DriverAvailable = 6,
        Resolved = 7,
        Cancelled = 8,
        Reopened = 9,
        Expired = 10
    }
}

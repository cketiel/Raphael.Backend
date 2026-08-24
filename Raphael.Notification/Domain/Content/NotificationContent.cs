namespace Raphael.Notification.Domain.Content;

/// <summary>
/// Text and parameters of one notification, already resolved for a given audience.
/// </summary>
/// <param name="Title">English title. This is what a push carries.</param>
/// <param name="Message">English body. This is what a push carries.</param>
/// <param name="MessageKey">Resource key the client applications translate.</param>
/// <param name="Parameters">Values the client needs to compose its own text.</param>
public sealed record NotificationContent(
    string Title,
    string Message,
    string MessageKey,
    IReadOnlyDictionary<string, string> Parameters);

namespace Raphael.Notification.Application.DTOs;

public sealed class UpdateBusinessEventDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool GeneratesNotification { get; init; }

    public bool IsActive { get; init; }
}
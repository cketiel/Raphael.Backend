namespace Raphael.Shared.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? UserName { get; }
        int? IntegratorId { get; }
        int? ProviderId { get; }
        bool IsMilanesInternal { get; }
    }
}

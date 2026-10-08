namespace Raphael.Shared.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? UserName { get; }
        int? IntegratorId { get; }
        int? ProviderId { get; }
        /// <summary>The caller's role id (claim <c>Role</c>). 1 is Admin; with an integrator, a clinic's admin.</summary>
        int? RoleId { get; }
        bool IsMilanesInternal { get; }
    }
}

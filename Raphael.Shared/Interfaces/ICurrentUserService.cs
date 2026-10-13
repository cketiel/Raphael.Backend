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
        /// <summary>A user of a Provider that works outside Raphael: never office, at most a clinic's reach.</summary>
        bool IsExternalProvider { get; }
        /// <summary>The account may only change its password (claim <c>MustChangePassword</c>).</summary>
        bool MustChangePassword { get; }
        /// <summary>
        /// The client company (tenant) whose data this caller is held to, from the <c>TenantId</c>
        /// claim. Null for Raphael's own office, its drivers, clinics and every user of a Provider
        /// the platform has not made a tenant (RE-049).
        /// </summary>
        int? TenantId { get; }
        /// <summary>Whether tenant isolation is switched on (<c>Tenancy:Enforce</c>). Off, nothing is filtered.</summary>
        bool TenancyEnforced { get; }
    }
}

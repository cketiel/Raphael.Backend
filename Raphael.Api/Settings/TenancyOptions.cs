namespace Raphael.Api.Settings
{
    /// <summary>
    /// Tenant isolation: whether a client company's users are held to their own data (RE-049).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only users of a Provider marked <c>IsTenant</c> are affected. Everybody else (Raphael's
    /// office, its drivers, clinics, external Providers) sees the rows with no owner, which is
    /// every row that existed before RE-049. So switching this on, with no tenant yet, changes
    /// nothing for anyone; it starts to matter the day the platform creates the first tenant.
    /// </para>
    /// <para>
    /// Off, nothing is filtered or stamped and the API behaves exactly as before RE-049, including
    /// for a tenant's users: do not hand out tenant accounts with this off.
    /// </para>
    /// <para>In Azure: <c>Tenancy__Enforce</c> = <c>true</c> | <c>false</c>.</para>
    /// </remarks>
    public sealed class TenancyOptions
    {
        public const string SectionName = "Tenancy";

        public bool Enforce { get; set; }
    }
}

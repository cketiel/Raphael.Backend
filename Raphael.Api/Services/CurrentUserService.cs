using Microsoft.Extensions.Options;
using Raphael.Api.Settings;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptionsMonitor<TenancyOptions> _tenancy;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<TenancyOptions> tenancy)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenancy = tenancy;
        }

        public int? UserId => GetClaimAsInt("UserId");
        public string? UserName => GetClaimValue("Username");
        public int? IntegratorId => GetClaimAsInt("UserIntegratorId");
        public int? ProviderId => GetClaimAsInt("UserProviderId");
        public int? RoleId => GetClaimAsInt("Role");
 
        public bool IsMilanesInternal => IntegratorId == null && ProviderId == null;
        public bool IsExternalProvider => GetClaimValue(PortalClaims.ProviderMode) == PortalClaims.External;
        public bool MustChangePassword => GetClaimValue(PortalClaims.MustChangePassword) == "true";
        public int? TenantId => GetClaimAsInt(PortalClaims.TenantId);
        public bool TenancyEnforced => _tenancy.CurrentValue.Enforce;

        private int? GetClaimAsInt(string type)
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
            return int.TryParse(claim, out var result) ? result : null;
        }
        private string? GetClaimValue(string type)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
        }
    }
}

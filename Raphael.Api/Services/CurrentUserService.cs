using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId => GetClaimAsInt("UserId");
        public int? IntegratorId => GetClaimAsInt("UserIntegratorId");
        public int? ProviderId => GetClaimAsInt("UserProviderId");
 
        public bool IsMilanesInternal => IntegratorId == null && ProviderId == null;

        private int? GetClaimAsInt(string type)
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
            return int.TryParse(claim, out var result) ? result : null;
        }
    }
}

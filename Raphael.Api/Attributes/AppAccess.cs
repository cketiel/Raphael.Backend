using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Raphael.Api.Services.Auth;
using Raphael.Api.Settings;

namespace Raphael.Api.Attributes
{
    /// <summary>
    /// The client applications an endpoint admits. Without it an endpoint admits only the Desktop.
    /// </summary>
    /// <remarks>
    /// On a method it replaces the one on its controller, so a method can widen or narrow it.
    /// List every application that calls the endpoint, the Desktop included when it does.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AllowAppsAttribute : Attribute
    {
        public AllowAppsAttribute(params string[] apps)
        {
            Apps = apps;
        }

        public IReadOnlyCollection<string> Apps { get; }
    }

    /// <summary>
    /// Holds every token to the endpoints of the application it was issued for (RE-048).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This inverts what used to be the rule: the API admitted any authenticated token anywhere and
    /// took access away endpoint by endpoint (<c>[NotForClinicUsers]</c>), so every new controller
    /// was born open to clinics, external Providers and drivers. Now an endpoint admits the
    /// Desktop unless it says otherwise, and a new one is closed to the rest without anybody
    /// having to remember.
    /// </para>
    /// <para>
    /// Anonymous endpoints (sign-in, refresh, the bot and the integration API with their keys) are
    /// not looked at. Neither are the SignalR hubs: they are not MVC, and every user application
    /// connects to them.
    /// </para>
    /// <para>
    /// The log line carries the route template, never the path: a path holds ids, and the query
    /// string can hold a patient's data.
    /// </para>
    /// </remarks>
    public sealed class AppAccessFilter : IAuthorizationFilter
    {
        private static readonly string[] DesktopOnly = { ClientApps.Desktop };

        private readonly IOptionsMonitor<AppAccessOptions> _options;
        private readonly ILogger<AppAccessFilter> _logger;

        public AppAccessFilter(IOptionsMonitor<AppAccessOptions> options, ILogger<AppAccessFilter> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var mode = _options.CurrentValue.Mode;
            if (mode == AppAccessMode.Off) return;

            // Already refused (no token), or an anonymous endpoint.
            if (context.Result != null) return;
            var metadata = context.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAllowAnonymous>().Any()) return;

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true) return;

            // The last one is the closest to the action: a method's wins over its controller's.
            var allowed = metadata.OfType<AllowAppsAttribute>().LastOrDefault()?.Apps ?? DesktopOnly;
            var app = ClientApps.Normalize(user.FindFirst(ClientApps.Claim)?.Value);

            if (allowed.Contains(app)) return;

            _logger.LogWarning(
                "App access {Outcome}: a {App} token called {Method} {Route}, which admits {Allowed}.",
                mode == AppAccessMode.Enforce ? "refused" : "would refuse",
                app,
                context.HttpContext.Request.Method,
                context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName,
                string.Join(",", allowed));

            if (mode == AppAccessMode.Enforce)
            {
                context.Result = new ObjectResult(new { code = "wrong_app", message = "This application cannot use this function." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}

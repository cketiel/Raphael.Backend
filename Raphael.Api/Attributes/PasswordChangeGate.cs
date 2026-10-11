using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Attributes
{
    /// <summary>
    /// Marks the one endpoint an account whose password must change may still call: changing it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AllowWhilePasswordChangeRequiredAttribute : Attribute
    {
    }

    /// <summary>
    /// Answers 403 <c>password_change_required</c> to every other call of an account whose password
    /// was set by somebody else, until its owner changes it.
    /// </summary>
    /// <remarks>
    /// A clinic creates the users of an external Provider and so knows their first password. That
    /// user sees the Provider's trips from every clinic: while the clinic still knows the password,
    /// the account must see nothing (PROVIDER_ACCESS_MODEL.md §3.4, P8). Enforced here, for every
    /// controller, because the portal hiding its screens would stop nobody holding the token.
    /// Anonymous endpoints (sign-in, refresh) carry no claim and pass.
    /// </remarks>
    public sealed class PasswordChangeGateFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            if (!currentUser.MustChangePassword) return;

            var allowed = context.ActionDescriptor.EndpointMetadata.OfType<AllowWhilePasswordChangeRequiredAttribute>().Any();
            if (allowed) return;

            context.Result = new ObjectResult(new { code = "password_change_required", message = "Change your password to continue." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}

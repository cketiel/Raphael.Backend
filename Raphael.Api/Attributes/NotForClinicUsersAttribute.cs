using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Attributes
{
    /// <summary>
    /// Answers 404 to a clinic (Booking Portal) user: the endpoint is for the office only.
    /// </summary>
    /// <remarks>
    /// A clinic holds its own token in the browser (the realtime hubs need it), so anything the
    /// API allows any session reaches a clinic too, BFF or not. A 404 and not a 403: a clinic has no
    /// business learning the endpoint exists. The office keeps whatever access it had.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NotForClinicUsersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

            if (currentUser.IntegratorId != null)
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;

namespace Raphael.Api.Attributes
{
    public class IntegrationApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Integration-ApiKey", out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key is missing.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<RaphaelContext>();
            var integrator = await db.Integrators
                .FirstOrDefaultAsync(i => i.ApiKey == extractedApiKey.ToString() && i.IsActive);

            if (integrator == null)
            {
                context.Result = new UnauthorizedObjectResult("Invalid or inactive API Key.");
                return;
            }

            // We save the integrator ID in the request context
            context.HttpContext.Items["IntegratorId"] = integrator.Id;
            await next();
        }
    }

    /*[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class IntegrationApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string APIKEYNAME = "X-RideCenter-ApiKey";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Result = new ContentResult() { StatusCode = 401, Content = "API Key was not provided" };
                return;
            }

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>("RideCenterConfig:ApiKey");

            if (!apiKey.Equals(extractedApiKey))
            {
                context.Result = new ContentResult() { StatusCode = 403, Content = "Unauthorized access" };
                return;
            }

            await next();
        }
    }*/
}
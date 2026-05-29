using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Raphael.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RideCenterApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string APIKEYNAME = "X-RideCenter-ApiKey";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Result = new ContentResult() { StatusCode = 401, Content = "Api Key no proporcionada" };
                return;
            }

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>("RideCenterConfig:ApiKey");

            if (!apiKey.Equals(extractedApiKey))
            {
                context.Result = new ContentResult() { StatusCode = 403, Content = "Acceso no autorizado" };
                return;
            }

            await next();
        }
    }
}
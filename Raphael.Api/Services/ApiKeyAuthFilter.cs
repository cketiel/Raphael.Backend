using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Raphael.Api.Models;

namespace Raphael.Api.Services
{
    public class ApiKeyAuthFilter : IAuthorizationFilter
    {
        private const string ApiKeyHeaderName = "X-Api-Key";
        private readonly BotSettings _botSettings;

        public ApiKeyAuthFilter(IOptions<BotSettings> botSettings)
        {
            _botSettings = botSettings.Value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // If the configuration did not load correctly
            if (_botSettings == null)
            {
                context.Result = new ObjectResult("Server configuration error (BotSettings missing)") { StatusCode = 500 };
                return;
            }

            // Check if the bot is active globally.
            if (!_botSettings.IsActive)
            {
                context.Result = new ContentResult() { StatusCode = 403, Content = "The bot service is currently disabled." };
                return;
            }

            // Check if the header exists
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key not provided.");
                return;
            }

            // Validate the API key
            if (!_botSettings.ApiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid API key.");
                return;
            }
        }
    }
}

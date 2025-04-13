using Azure.Core;
using Microsoft.AspNetCore.Mvc;

namespace Meditrans.Gateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] object credentials)
    {
        //var client = _httpClientFactory.CreateClient();
        //var response = await client.PostAsJsonAsync("https://localhost:7151/api/auth/login", credentials);

        var client = _httpClientFactory.CreateClient("UsersService");
        var response = await client.PostAsJsonAsync("api/Auth/login", credentials);


        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }
}

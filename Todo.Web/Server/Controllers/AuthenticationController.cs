using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Todo.Web.Shared.Models;
using AuthenticationService = Todo.Web.Server.Services.AuthenticationService;

namespace Todo.Web.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthenticationController(AuthenticationService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] UserInfo userInfo)
    {
        var token = await authService.RegisterUserAsync(userInfo);

        return token is null ? Unauthorized() : SignIn(userInfo, token);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser([FromBody] UserInfo userInfo)
    {
        var token = await authService.LoginUserAsync(userInfo);

        return token is null ? Unauthorized() : SignIn(userInfo, token);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutUser()
    {
        await authService.LogoutUserAsync(HttpContext);

        return Ok();
    }

    [HttpGet("login/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var properties = new AuthenticationProperties { RedirectUri = $"/api/auth/signin/{provider}" };

        return Challenge(properties, provider);
    }

    [HttpGet("signin/{provider}")]
    public async Task<IActionResult> ExternalSignIn(string provider)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalScheme");

        if (result.Succeeded)
        {
            var principal = result.Principal;
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var name = (principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name)!;
            var token = await authService.ExternalSignInAsync(provider, new ExternalUserInfo { Username = name, KeyProvider = id });

            if (token is not null)
            {
                return SignIn(id, name, token, provider);
            }
        }

        await HttpContext.SignOutAsync("ExternalScheme");

        return Redirect("/");
    }

    private IActionResult SignIn(UserInfo userInfo, string token)
    {
        return SignIn(userInfo.Username, userInfo.Username, token, providerName: null);
    }

    private IActionResult SignIn(string userId, string userName, string token, string? providerName)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        var tokens = new[]
        {
            new Microsoft.AspNetCore.Authentication.AuthenticationToken { Name = "access_token", Value = token }
        };

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim(ClaimTypes.Name, userName));

        if (providerName is not null)
        {
            properties.SetParameter("provider", providerName);
        }

        properties.StoreTokens(tokens);

        return SignIn(new ClaimsPrincipal(identity), properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Services;

public class AuthenticationService(AuthenticationApiService client)
{
    public async Task<string?> RegisterUserAsync(UserInfo userInfo)
    {
        return await client.CreateUserAsync(userInfo);
    }

    public async Task<string?> LoginUserAsync(UserInfo userInfo)
    {
        return await client.GetTokenAsync(userInfo);
    }

    public async Task LogoutUserAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<string?> ExternalSignInAsync(string provider, ExternalUserInfo externalUserInfo)
    {
        return await client.GetOrCreateUserAsync(provider, externalUserInfo);
    }
}

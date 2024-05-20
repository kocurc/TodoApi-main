using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Todo.Web.Server.Extensions;
using Todo.Web.Shared.SharedClasses;

namespace Todo.Web.Server.Authentication;

public static class AuthApi
{
    public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth");

        _ = group.MapPost("register", async (UserInfo userInfo, AuthClient client) =>
        {
            // Retrieve the access token given the user info
            var token = await client.CreateUserAsync(userInfo);

            if (token is null)
            {
                return Results.Unauthorized();
            }

            return SignIn(userInfo, token);
        });
        _ = group.MapPost("login", async (UserInfo userInfo, AuthClient client) =>
        {
            var token = await client.GetTokenAsync(userInfo);

            return token is null ? Results.Unauthorized() : SignIn(userInfo, token);
        });
        _ = group.MapPost("logout", async (context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            })
            .RequireAuthorization();
        _ = group.MapGet("login/{provider}", (string provider) => Results.Challenge(
            properties: new AuthenticationProperties { RedirectUri = $"/auth/signin/{provider}" },
            authenticationSchemes: [provider]));
        _ = group.MapGet("signin/{provider}", async (string provider, AuthClient client, HttpContext context) =>
        {
            var result = await context.AuthenticateAsync(AuthenticationSchemes.ExternalScheme);

            if (result.Succeeded)
            {
                var principal = result.Principal;
                var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var name = (principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name)!;
                var token = await client.GetOrCreateUserAsync(provider, new() { Username = name, KeyProvider = id });

                if (token is not null)
                {
                    await SignIn(id, name, token, provider).ExecuteAsync(context);
                }
            }

            await context.SignOutAsync(AuthenticationSchemes.ExternalScheme);

            return Results.Redirect("/");
        });

        return group;
    }

    private static IResult SignIn(UserInfo userInfo, string token)
    {
        return SignIn(userInfo.Username, userInfo.Username, token, providerName: null);
    }

    private static IResult SignIn(string userId, string userName, string token, string? providerName)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        var tokens = new[]
        {
            new Microsoft.AspNetCore.Authentication.AuthenticationToken { Name = TokenNames.AccessToken, Value = token }
        };

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim(ClaimTypes.Name, userName));

        if (providerName is not null)
        {
            properties.SetExternalProvider(providerName);
        }

        properties.StoreTokens(tokens);

        return Results.SignIn(new ClaimsPrincipal(identity),
            properties: properties,
            authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

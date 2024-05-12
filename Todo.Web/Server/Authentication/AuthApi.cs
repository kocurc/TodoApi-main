﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Todo.Web.Shared.SharedClasses;

namespace Todo.Web.Server.Authentication
{
    public static class AuthApi
    {
        public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/auth");

            group.MapPost("register", async (UserInfo userInfo, AuthClient client) =>
            {
                // Retrieve the access token given the user info
                var token = await client.CreateUserAsync(userInfo);

                if (token is null)
                {
                    return Results.Unauthorized();
                }

                return SignIn(userInfo, token);
            });

            group.MapPost("login", async (UserInfo userInfo, AuthClient client) =>
            {
                var token = await client.GetTokenAsync(userInfo);

                if (token is null)
                {
                    return Results.Unauthorized();
                }

                return SignIn(userInfo, token);
            });

            group.MapPost("logout", async (context) =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // TODO: Support remote logout
                    // If this is an external login then use it
                    //var result = await context.AuthenticateAsync();
                    //if (result.Properties?.GetExternalProvider() is string providerName)
                    //{
                    //    await context.SignOutAsync(providerName, new() { RedirectUri = "/" });
                    //}
                })
                .RequireAuthorization();

            // External login
            group.MapGet("login/{provider}", (string provider) =>
            {
                // Trigger the external login flow by issuing a challenge with the provider name.
                // This name maps to the registered authentication scheme names in AuthenticationExtensions.cs
                return Results.Challenge(
                    properties: new() { RedirectUri = $"/auth/signin/{provider}" },
                    authenticationSchemes: new[] { provider });
            });

            group.MapGet("signin/{provider}", async (string provider, AuthClient client, HttpContext context) =>
            {
                // Grab the login information from the external login dance
                var result = await context.AuthenticateAsync(AuthenticationSchemes.ExternalScheme);

                if (result.Succeeded)
                {
                    var principal = result.Principal;

                    var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

                    // TODO: We should have the user pick a user name to complete the external login dance
                    // for now we'll prefer the email address
                    var name = (principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name)!;

                    var token = await client.GetOrCreateUserAsync(provider, new() { Username = name, KeyProvider = id });

                    if (token is not null)
                    {
                        // Write the login cookie
                        await SignIn(id, name, token, provider).ExecuteAsync(context);
                    }
                }

                // Delete the external cookie
                await context.SignOutAsync(AuthenticationSchemes.ExternalScheme);

                // TODO: Handle the failure somehow

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
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
            identity.AddClaim(new Claim(ClaimTypes.Name, userName));

            var properties = new AuthenticationProperties();

            // Store the external provider name so we can do remote sign out
            if (providerName is not null)
            {
                properties.SetExternalProvider(providerName);
            }

            var tokens = new[]
            {
                new Microsoft.AspNetCore.Authentication.AuthenticationToken { Name = TokenNamesOK.AccessToken, Value = token }
            };

            properties.StoreTokens(tokens);


            return Results.SignIn(new ClaimsPrincipal(identity),
                properties: properties,
                authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

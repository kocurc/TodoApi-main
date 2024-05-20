using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Database;
using CurrentUser = Todo.Web.Server.Authorization.CurrentUser;
using Todo.Web.Shared.SharedClasses;
using Microsoft.AspNetCore.Identity;
using Todo.Web.Server.Users;
using AuthenticationToken = Todo.Web.Shared.SharedClasses.AuthenticationToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Todo.Web.Server.Extensions;

public static class IEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTodos(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/todos");

        group.RequireAuthorization();
        group.WithTags("Todos");
        group.RequireAuthorization(pb => pb.RequireCurrentUser())
            .AddOpenApiSecurityRequirement();
        group.RequirePerUserRateLimit();
        group.WithParameterValidation(typeof(TodoItem));
        group.MapGet("/", async (TodoDbContext db, CurrentUser owner) =>
        {
            return await db.Todos.Where(todo => todo.OwnerId == owner.Id).Select(t => t.AsTodoItem()).AsNoTracking().ToListAsync();
        });
        group.MapGet("/{id:int}", async Task<Results<Ok<TodoItem>, NotFound>> (TodoDbContext db, int id, CurrentUser owner) =>
        {
            return await db.Todos.FindAsync(id) switch
            {
                { } todo when todo.OwnerId == owner.Id || owner.IsAdmin => TypedResults.Ok(todo.AsTodoItem()),
                _ => TypedResults.NotFound()
            };
        });
        group.MapPost("/", async Task<Created<TodoItem>> (TodoDbContext db, TodoItem newTodo, CurrentUser owner) =>
        {
            var todo = new Todos.Todo
            {
                Title = newTodo.Title,
                OwnerId = owner.Id
            };

            db.Todos.Add(todo);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/todos/{todo.Id}", todo.AsTodoItem());
        });
        group.MapPut("/{id}", async Task<Results<Ok, NotFound, BadRequest>> (TodoDbContext db, int id, TodoItem todo, CurrentUser owner) =>
        {
            if (id != todo.Id)
            {
                return TypedResults.BadRequest();
            }

            var rowsAffected = await db.Todos.Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
                .ExecuteUpdateAsync(updates =>
                    updates.SetProperty(t => t.IsComplete, todo.IsComplete)
                        .SetProperty(t => t.Title, todo.Title));

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });
        group.MapDelete("/{id:int}", async Task<Results<NotFound, Ok>> (TodoDbContext db, int id, CurrentUser owner) =>
        {
            var rowsAffected = await db.Todos.Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
                .ExecuteDeleteAsync();

            return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
        });

        return group;
    }

    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/users");

        _ = group.WithTags("Users");
        _ = group.WithParameterValidation(typeof(UserInfo), typeof(ExternalUserInfo));
        _ = group.MapPost("/", async Task<Results<Ok, ValidationProblem>> (UserInfo newUser, UserManager<TodoUser> userManager) =>
        {
            var result = await userManager.CreateAsync(new TodoUser { UserName = newUser.Username }, newUser.Password);

            if (result.Succeeded)
            {
                return TypedResults.Ok();
            }

            return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        });
        _ = group.MapPost("/token", async Task<Results<BadRequest, Ok<AuthenticationToken>>> (UserInfo userInfo, UserManager<TodoUser> userManager, ITokenService tokenService) =>
        {
            var user = await userManager.FindByNameAsync(userInfo.Username);

            if (user is null || !await userManager.CheckPasswordAsync(user, userInfo.Password))
            {
                return TypedResults.BadRequest();
            }

            return TypedResults.Ok(new AuthenticationToken(tokenService.GenerateToken(user.UserName!)));
        });
        _ = group.MapPost("/token/{provider}", async Task<Results<Ok<AuthenticationToken>, ValidationProblem>> (string provider, ExternalUserInfo userInfo, UserManager<TodoUser> userManager, ITokenService tokenService) =>
        {
            var user = await userManager.FindByLoginAsync(provider, userInfo.KeyProvider);

            var result = IdentityResult.Success;

            if (user is null)
            {
                user = new TodoUser() { UserName = userInfo.Username };

                result = await userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    result = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, userInfo.KeyProvider, displayName: null));
                }
            }

            if (result.Succeeded)
            {
                return TypedResults.Ok(new AuthenticationToken(tokenService.GenerateToken(user.UserName!)));
            }

            return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        });

        return group;
    }

    public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth");

        _ = group.MapPost("register", async (UserInfo userInfo, AuthClient client) =>
        {
            var token = await client.CreateUserAsync(userInfo);

            return token is null ? Results.Unauthorized() : SignIn(userInfo, token);
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

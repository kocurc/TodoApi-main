using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Todo.Web.Server.Authorization;

namespace Todo.Web.Server.Extensions;

public static class AuthorizationBuilderExtensions
{
    public static AuthorizationBuilder AddCurrentUserHandler(this AuthorizationBuilder builder)
    {
        builder.Services.AddScoped<IAuthorizationHandler, CurrentUserAuthorizationHandler>();

        return builder;
    }

    public static AuthorizationPolicyBuilder RequireCurrentUser(this AuthorizationPolicyBuilder builder)
    {
        return builder.RequireAuthenticatedUser().AddRequirements(new CurrentUserRequirement());
    }
}

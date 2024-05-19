using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Todo.Web.Server.Authorization;

// This authorization handler verifies that the user exists even if there's a valid token
public class CurrentUserAuthorizationHandler(CurrentUser currentUser) : AuthorizationHandler<CheckCurrentUserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CheckCurrentUserRequirement requirement)
    {
        // TODO: Check if the user is locked out as well

        if (currentUser.User is not null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

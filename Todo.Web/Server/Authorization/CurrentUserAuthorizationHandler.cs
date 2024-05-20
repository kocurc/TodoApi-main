using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Todo.Web.Server.Authorization;

public class CurrentUserAuthorizationHandler(CurrentUser currentUser) : AuthorizationHandler<CurrentUserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CurrentUserRequirement requirement)
    {

        if (currentUser.User is not null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

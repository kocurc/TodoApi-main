using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Todo.Web.Server.Authorization;

public sealed class ClaimsTransformation(CurrentUser currentUser, UserManager<IdentityUser> userManager) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        currentUser.Principal = principal;

        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } name)
        {
            currentUser.User = await userManager.FindByNameAsync(name);
        }

        return principal;

    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Todo.Web.Server.Authorization;

public class CurrentUser
{
    public IdentityUser? User { get; set; }

    public ClaimsPrincipal Principal { get; set; } = default!;

    public string Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public bool IsAdmin => Principal.IsInRole("admin");
}

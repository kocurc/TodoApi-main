using System.Security.Claims;
using Todo.Web.Server.Users;

namespace Todo.Web.Server.Authorization;

public class CurrentUser
{
    public TodoUser? User { get; set; }

    public ClaimsPrincipal Principal { get; set; } = default!;

    public string Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public bool IsAdmin => Principal.IsInRole("admin");
}

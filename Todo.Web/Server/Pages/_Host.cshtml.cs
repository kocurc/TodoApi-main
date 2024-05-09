using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Todo.Web.Server.Authentication;

namespace Todo.Web.Server.Pages
{
    public class IndexModel(ExternalProviders socialProviders) : PageModel
    {
        public string[] ProviderNames { get; set; } = default!;
        public string? CurrentUserName { get; set; }

        public async Task OnGet()
        {
            ProviderNames = await socialProviders.GetProviderNamesAsync();
            CurrentUserName = User.Identity!.Name;
        }
    }
}

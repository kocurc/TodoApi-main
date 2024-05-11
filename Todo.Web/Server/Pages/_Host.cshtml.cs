using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Todo.Web.Server.Authentication;

namespace Todo.Web.Server.Pages
{
    // OK. The class serves as the model for the _Host.cshtml page. PageModel is a base class for Razor Pages.
    public class IndexModel(ExternalProviders socialProviders) : PageModel
    {
        // ! is a null-forgiving operator. It tells the compiler that the property will never be null.
        public string[] ProviderNames { get; set; } = default!;
        // ? is a null-conditional operator. It tells the compiler that the property can be null.
        public string? CurrentUserName { get; set; }

        // The OnGet method is an event called when the page is requested.
        public async Task OnGet()
        {
            ProviderNames = await socialProviders.GetProviderNamesAsync();
            // ! is a null-forgiving operator. It tells the compiler that the User.Identity will never be null.
            CurrentUserName = User.Identity!.Name;
        }
    }
}

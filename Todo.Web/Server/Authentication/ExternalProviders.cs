using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Todo.Web.Server.Authentication;

// OK. External providers (also known as social providers) are services that can authenticate a user
// for your application. Examples: Google, Facebook, Twitter, OAuth, OpenID Connect, etc.
// IAuthenticationSchemeProvider is an interface provided by ASP.NET Core
// that allows you to retrieve authentication scheme information.
public class ExternalProviders(IAuthenticationSchemeProvider schemeProvider)
{
    // Stores the provider names.
    private Task<string[]>? _providerNames;

    // Gets the provider names.
    public Task<string[]> GetProviderNamesAsync()
    {
        return _providerNames ??= GetProviderNamesAsyncCore();
    }

    // Gets the provider names. This is the core implementation.
    private async Task<string[]> GetProviderNamesAsyncCore()
    {
        List<string>? providerNames = [];
        var schemes = await schemeProvider.GetAllSchemesAsync();

        providerNames.AddRange(from scheme in schemes where scheme.Name != CookieAuthenticationDefaults.AuthenticationScheme && scheme.Name != AuthenticationSchemes.ExternalScheme select scheme.Name);

        return providerNames?.ToArray() ?? [];
    }
}
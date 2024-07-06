using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Todo.Web.Server.Authentication;

public class ExternalProviders(IAuthenticationSchemeProvider schemeProvider)
{
    private Task<string[]>? _providerNames;

    public Task<string[]> GetProviderNamesAsync()
    {
        return _providerNames ??= GetProviderNamesAsyncCore();
    }

    private async Task<string[]> GetProviderNamesAsyncCore()
    {
        List<string>? providerNames = [];
        var schemes = await schemeProvider.GetAllSchemesAsync();

        providerNames.AddRange(from scheme in schemes where scheme.Name != CookieAuthenticationDefaults.AuthenticationScheme && scheme.Name != AuthenticationSchemes.ExternalScheme select scheme.Name);

        return providerNames?.ToArray() ?? [];
    }
}

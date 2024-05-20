using Microsoft.AspNetCore.Authentication;

namespace Todo.Web.Server.Extensions;

public static class AuthenticationPropertiesExtensions
{
    private const string ExternalProviderKey = "ExternalProviderName";

    public static void SetExternalProvider(this AuthenticationProperties properties, string? providerName)
    {
        properties.SetString(ExternalProviderKey, providerName);
    }
}

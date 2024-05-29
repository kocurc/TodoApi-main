using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Todo.Web.Shared.SharedClasses;
using AuthenticationToken = Todo.Web.Shared.SharedClasses.AuthenticationToken;
using ExternalUserInfo = Todo.Web.Shared.SharedClasses.ExternalUserInfo;

namespace Todo.Web.Server.Authentication;

public class UserAuthenticationClient(HttpClient client)
{
    public async Task<string?> GetTokenAsync(UserInfo userInfo)
    {
        var response = await client.PostAsJsonAsync("users/token", userInfo);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        return token?.Token;
    }

    public async Task<string?> CreateUserAsync(UserInfo userInfo)
    {
        var response = await client.PostAsJsonAsync("users", userInfo);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await GetTokenAsync(userInfo);
    }

    public async Task<string?> GetOrCreateUserAsync(string provider, ExternalUserInfo userInfo)
    {
        var response = await client.PostAsJsonAsync($"users/token/{provider}", userInfo);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        return token?.Token;
    }
}

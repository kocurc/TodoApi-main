using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Todo.Web.Shared.Models;
using AuthenticationToken = Todo.Web.Shared.Models.AuthenticationToken;
using ExternalUserInfo = Todo.Web.Shared.Models.ExternalUserInfo;

namespace Todo.Web.Server.Services;

public class AuthenticationApiService
{
    private readonly HttpClient _client;

    public AuthenticationApiService(HttpClient client)
    {
        _client = client;
    }

    public async Task<string?> GetTokenAsync(UserInfo userInfo)
    {
        var response = await _client.PostAsJsonAsync("users/token", userInfo);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        return token?.Token;
    }

    public async Task<string?> CreateUserAsync(UserInfo userInfo)
    {
        try
        {
            Console.WriteLine("Starting register request...");
            var response = await _client.PostAsJsonAsync("users", userInfo);
            Console.WriteLine("Register request sent.");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Register request failed.");
                return null;
            }

            Console.WriteLine("Register request succeeded, getting token...");
            return await GetTokenAsync(userInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            throw;
        }
    }

    public async Task<string?> GetOrCreateUserAsync(string provider, ExternalUserInfo userInfo)
    {
        var response = await _client.PostAsJsonAsync($"users/token/{provider}", userInfo);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        return token?.Token;
    }
}

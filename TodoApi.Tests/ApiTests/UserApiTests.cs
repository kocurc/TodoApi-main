using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Todo.Web.Server.Users;
using Todo.Web.Shared.SharedClasses;
using Xunit;

namespace Todo.Tests.ApiTests;

public class UserApiTests
{
    [Fact]
    public async Task CanCreateAUser()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users", new UserInfo { Username = "todouser", Password = "@pwd" });
        var user = db.Users.Single();

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(user);
        Assert.Equal("todouser", user.UserName);
    }

    private static readonly string[] Expected = ["The Password field is required."];
    private static readonly string[] ExpectedDetails = ["The Username field is required."];

    [Fact]
    public async Task MissingUserOrPasswordReturnsBadRequest()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users", new UserInfo { Username = "todouser", Password = "" });
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Equal(Expected, problemDetails.Errors["Password"]);

        response = await client.PostAsJsonAsync("/users", new UserInfo { Username = "", Password = "password" });
        problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Equal(ExpectedDetails, problemDetails.Errors["Username"]);
    }

    [Fact]
    public async Task MissingUsernameOrProviderKeyReturnsBadRequest()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users/token/Google", new ExternalUserInfo { Username = "todouser" });
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Equal(new[] { $"The {nameof(ExternalUserInfo.KeyProvider)} field is required." }, problemDetails.Errors[nameof(ExternalUserInfo.KeyProvider)]);

        response = await client.PostAsJsonAsync("/users/token/Google", new ExternalUserInfo { KeyProvider = "somekey" });
        problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.NotEmpty(problemDetails.Errors);
        Assert.Equal(new[] { $"The Username field is required." }, problemDetails.Errors["Username"]);
    }

    [Fact]
    public async Task CanGetATokenForValidUser()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        await application.CreateUserAsync("todouser", "p@assw0rd1");
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users/token", new UserInfo { Username = "todouser", Password = "p@assw0rd1" });
        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(token);

        var req = new HttpRequestMessage(HttpMethod.Get, "/todos");

        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        response = await client.SendAsync(req);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CanGetATokenForExternalUser()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users/token/Google", new ExternalUserInfo { Username = "todouser", KeyProvider = "1003" });
        var token = await response.Content.ReadFromJsonAsync<AuthenticationToken>();

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(token);

        var req = new HttpRequestMessage(HttpMethod.Get, "/todos");

        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        response = await client.SendAsync(req);

        using var scope = application.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TodoUser>>();
        var user = await userManager.FindByLoginAsync("Google", "1003");

        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(user);
        Assert.Equal("todouser", user.UserName);
    }

    [Fact]
    public async Task BadRequestForInvalidCredentials()
    {
        await using var application = new TodoApplication();
        await using var db = application.CreateTodoDbContext();
        await application.CreateUserAsync("todouser", "p@assw0rd1");
        var client = application.CreateClient();
        var response = await client.PostAsJsonAsync("/users/token", new UserInfo { Username = "todouser", Password = "prd1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

using System.Net;
using Ganss.Xss;
using Shouldly;
using Todo.Web.Client;
using TodoApi.Tests.Builders;
using Xunit;

namespace TodoApi.Tests.Todo.Web.Client.Tests;

public class TodoClientTests
{
    private readonly HttpClientMockBuilder _httpClientMockBuilder;

    public TodoClientTests()
    {
        _httpClientMockBuilder = new HttpClientMockBuilder();
    }

    [Fact]
    public async void GivenUnsanitizedUserNameAndPassword_ThenShouldSanitizeUserNameAndPassword()
    {
        // Arrange
        var userLoginData = new UserLoginData("<script>alert('xss')</script>John", "<img src=\"javascript:alert('XSS');\">Password");
        var todoClient = new TodoClient(_httpClientMockBuilder.WithResponse(HttpStatusCode.OK).Build().Object, new HtmlSanitizer());

        // Act
        var loginResponse = await todoClient.LoginAsync(userLoginData.UserName, userLoginData.Password);

        // Assert
        loginResponse.ShouldBeTrue();
    }
}

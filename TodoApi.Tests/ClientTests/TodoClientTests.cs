using System.Net;
using System.Threading.Tasks;
using Ganss.Xss;
using Shouldly;
using Todo.Tests.Builders;
using Todo.Web.Client;
using Xunit;

namespace Todo.Tests.ClientTests;

public class TodoClientTests
{
    private readonly HttpClientMockBuilder _httpClientMockBuilder = new();

    [Fact]
    public async Task GivenUnsanitizedUserNameAndPassword_ThenShouldSanitizeUserNameAndPassword()
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
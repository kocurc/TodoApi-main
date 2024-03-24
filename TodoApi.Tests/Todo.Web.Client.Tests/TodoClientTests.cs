using Ganss.Xss;
using Shouldly;
using Todo.Web.Client;

namespace TodoApi.Tests.Todo.Web.Client.Tests
{
    public class TodoClientTests
    {
        [Fact]
        public async void GivenUnsanitizedUserNameAndPassword_ThenShouldSanitizeUserNameAndPassword()
        {
            var htmlSanitizer = new HtmlSanitizer();
            var httpClient = new HttpClient();
            var unsanitizedUserName = "<script>alert('xss')</script>John";
            var unsanitizedPassword = "<img src=\"javascript:alert('XSS');\">Password";
            var todoClient = new TodoClient(httpClient, htmlSanitizer);
            var response = await todoClient.LoginAsync(unsanitizedUserName, unsanitizedPassword);

            response.ShouldBeTrue();
        }
    }
}

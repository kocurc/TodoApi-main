using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Web.Server.Authentication
{
    public static class AuthenticationExtensionsTodoApi
    {
        public static WebApplicationBuilder AddAuthenticationTodoApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication().AddJwtBearer();
            return builder;

        }
    }
}

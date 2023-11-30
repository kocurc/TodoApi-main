namespace TodoApi.Common.Authentication;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication().AddJwtBearer();

        return builder;
    }
}

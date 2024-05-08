using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TodoApi.Authentication;
using TodoApi.Authorization;
using TodoApi.Extensions;
using TodoApi.Todos;
using TodoApi.Users;

namespace TodoApi;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure auth
        builder.AddAuthentication();
        builder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();

        // Add the service to generate JWT tokens
        builder.Services.AddTokenService();

        // Configure the database
        var connectionString = builder.Configuration.GetConnectionString("Todos") ?? "Data Source=.db/Todos.db";
        builder.Services.AddSqlite<TodoDbContext>(connectionString);

        // Configure identity
        builder.Services.AddIdentityCore<TodoUser>()
            .AddEntityFrameworkStores<TodoDbContext>();

        // State that represents the current user from the database *and* the request
        builder.Services.AddCurrentUser();

        // Configure Open API
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o => o.InferSecuritySchemes());

        // Configure rate limiting
        builder.Services.AddRateLimiting();

        // Configure OpenTelemetry
        builder.AddOpenTelemetry();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders
        app.UseSecurityHeaders(policies => policies
            .AddFrameOptionsDeny() // Prevents the page from being displayed in an iframe or object.
            .AddXssProtectionBlock() // Enables XSS filtering. If a cross-site scripting attack is detected, the browser will stop rendering the page.
            .AddContentTypeOptionsNoSniff() // Prevents the browser from MIME-sniffing a response away from the declared content-type.
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(
                maxAgeInSeconds: 60 * 60 * 24 * 365) // Enforces secure (HTTP over SSL/TLS) connections to the server. This header also includes subdomains and is set for one year.
            .AddReferrerPolicyStrictOriginWhenCrossOrigin() // Provides the origin of the document as the referrer when the protocol security level stays the same or improves (HTTP to HTTPS), but doesn't send it for a less secure destination (HTTPS to HTTP).
            .RemoveServerHeader() // Removes the `Server` header from the response.
            .AddContentSecurityPolicy(builder => // Sets the `Content-Security-Policy` header, which helps prevent a wide range of content injection attacks such as cross-site scripting (XSS).
            {
                builder.AddObjectSrc().None(); // Prevents the page from loading any objects.
                builder.AddFormAction().Self(); // Allows form submissions only to the same origin.
                builder.AddFrameAncestors().None(); // Prevents the page from being embedded in an iframe or object on any site.
            })
            .AddCrossOriginOpenerPolicy(builder => // Sets the `Cross-Origin-Opener-Policy` header, which isolates your site from other sites in a new browsing context.
            {
                builder.SameOrigin(); // The builder property `SameOrigin()` allows the policy to only apply to the same origin.
            })
            .AddCrossOriginEmbedderPolicy(builder => // Sets the `Cross-Origin-Embedder-Policy` header, which requires all resources to be loaded securely and ensures your site is isolated from other sites in a new browsing context.
            {
                builder.RequireCorp(); // The builder property `RequireCorp()` requires the policy to apply to all corporate networks.
            })
            .AddCrossOriginResourcePolicy(builder => // Sets the `Cross-Origin-Resource-Policy` header, which controls the set of origins that are allowed to get your site's resources.
            {
                builder.SameOrigin(); // The builder property `SameOrigin()` allows the policy to only apply to the same origin.
            })
            .AddCustomHeader("X-My-Test-Header", "Test header value")); // Adds a custom header named `X-My-Test-Header` with the value `Test header value` only for testing purpose.

        app.UseRateLimiter();
        app.Map("/", () => Results.Redirect("/swagger"));

        // Configure the APIs
        app.MapTodos();
        app.MapUsers();

        // Configure the prometheus endpoint for scraping metrics
        app.MapPrometheusScrapingEndpoint();
        // NOTE: This should only be exposed on an internal port!
        // .RequireHost("*:9100");

        app.Run();
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Database;
using Todo.Web.Server.Extensions;
using Todo.Web.Server.Users;

namespace Todo.Web.Server;

// Entry point for application and its services
public class Program
{
    public static void Main(string[] args)
    {
        // Initialize default services, logging and configuration sources
        var webApplicationBuilder = WebApplication.CreateBuilder(args);
        var databaseConnectionString = webApplicationBuilder.Configuration.GetConnectionString("SQLiteConnectionString") ?? "Data Source=.db/IEndpointRouteBuilderExtensions.db";

        // ADD SERVICES TO THE APPLICATION
        // Configures who you are
        webApplicationBuilder.AddAuthentication();
        // Configures logging, distributed tracing and scraping metrics, for instance using Prometheus
        webApplicationBuilder.AddOpenTelemetry();
        // Configures what you can do
        webApplicationBuilder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();
        // Use SQLLite as the database
        webApplicationBuilder.Services.AddSqlite<TodoDbContext>(databaseConnectionString);
        // Add support for Razor C#-HTML pages
        webApplicationBuilder.Services.AddRazorPages();
        // Adds per-user rate limiting to the application, with a limit of 100 requests every 10 seconds
        webApplicationBuilder.Services.AddRateLimiting();

        // ADD SINGLETON SERVICES. THEY CREATED ONCE PER APPLICATION AND EVERY REQUEST USES THE SAME INSTANCE
        // Add the service to generate JWT tokens
        webApplicationBuilder.Services.AddSingleton<ITokenService, TokenService>();

        //----------------------------------------------------------
        webApplicationBuilder.Services.AddEndpointsApiExplorer();
        webApplicationBuilder.Services.AddSwaggerGen(o => o.InferSecuritySchemes());
        // Configure identity
        webApplicationBuilder.Services.AddIdentityCore<TodoUser>()
            .AddEntityFrameworkStores<TodoDbContext>();
        // State that represents the current user from the database *and* the request
        webApplicationBuilder.Services.AddCurrentUser();
        var app = webApplicationBuilder.Build();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFallbackToPage("/_Host");
        // Configure the prometheus endpoint for scraping metrics
        // NOTE: This should only be exposed on an internal port!
        // .RequireHost("*:9100");
        app.MapPrometheusScrapingEndpoint();
        // Configure the APIs
        app.MapAuth();
        app.MapTodos("null");
        // app.MapTodos();
        app.MapUsers();
        app.UseRateLimiter();
        // app.Map("/", () => Results.Redirect("/swagger"));
        // app.Map("/", () => Results.Redirect("/"));
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
        // Run application
        app.Run();
    }
}

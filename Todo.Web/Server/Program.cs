using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Authorization;
using Todo.Web.Server.Database;
using Todo.Web.Server.Extensions;
using Todo.Web.Server.Middleware;
using Todo.Web.Server.Services;
using AuthenticationService = Todo.Web.Server.Services.AuthenticationService;

namespace Todo.Web.Server;

// Entry point for application and its services
public class Program
{
    public static void Main(string[] args)
    {
        // Initialize default services, logging and configuration sources
        var webApplicationBuilder = WebApplication.CreateBuilder(args);
        var databaseConnectionString =
            webApplicationBuilder.Configuration.GetConnectionString("SQLiteConnectionString") ??
            "Data Source=.db/IEndpointRouteBuilderExtensions.db";

        // ---------------------------------------------------------
        // ALL ADD*() METHODS ADD SERVICES TO THE DEPENDENCY INJECTION CONTAINER
        // Registers the controllers in the application
        webApplicationBuilder.Services.AddControllers();
        // Configures who you are
        webApplicationBuilder.AddAuthentication();
        // Configures logging, distributed tracing and scraping metrics, for instance using Prometheus
        webApplicationBuilder.AddOpenTelemetry();
        // Adds service that can generate Swagger documents for your API.
        // InferSecuritySchemes infers the security schemes from the authorization policies - JWT Bearer in this case
        webApplicationBuilder.Services.AddSwaggerGen(swaggerGenOptions => swaggerGenOptions.InferSecuritySchemes());
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
        // Enable API Explorer for OpenAPI documentation for the endpoints defined in your application using the Map methods,
        // like MapPost in the IEndpointRouteBuilder interface
        webApplicationBuilder.Services.AddEndpointsApiExplorer();
        // AddIdentityCore - Add the core identity services to the Dependency Injection container
        // that support UI login functionalities
        // AddEntityFrameworkStores - Adds Entity Framework stores for user data storing.
        // TodoDbContext is DbContext that will interact with the database
        webApplicationBuilder.Services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<TodoDbContext>();
        // ADD SCOPED SERVICES. A NEW INSTANCE IS CREATED FOR EVERY REQUEST
        // Services that contain business logic for their controllers
        webApplicationBuilder.Services.AddScoped<TodoService>();
        webApplicationBuilder.Services.AddScoped<AuthenticationService>();
        webApplicationBuilder.Services.AddScoped<UserService>();
        // UserAuthenticationClient - Service that handles the user authentication Tasks
        webApplicationBuilder.Services.AddScoped<AuthenticationApiService>();
        // CurrentUser - Service that holds the current user information
        webApplicationBuilder.Services.AddScoped<CurrentUser>();
        // ClaimsTransformation - Service that provides a way to apply custom logic to a user's claims
        // after they have been authenticated
        webApplicationBuilder.Services.AddScoped<IClaimsTransformation, ClaimsTransformation>();
        // Registers IHttpClientFactory that handles HttpClient instances.
        // HttpClient is used to send HTTP requests and receive HTTP responses
        webApplicationBuilder.Services.AddHttpClient();
        // ADD TRANSIENT SERVICES. A NEW INSTANCE IS CREATED EVERY TIME THE SERVICE IS REQUESTED
        // NONE FOR NOW
        // Build method creates a WebApplication object, which represents configured web application and its services.
        // Later you can set up middleware - Use... and Map... methods to configure the application's request pipeline
        var webApplication = webApplicationBuilder.Build();
        // ---------------------------------------------------------
        // ALL USE*() METHODS ADD MIDDLEWARE TO THE REQUEST PIPELINE
#if DEBUG
        // UseWebAssemblyDebugging - Adds middleware that enables debugging of Blazor WebAssembly application,
        // like adding a breakpoint in the code or inspect variables
        webApplication.UseWebAssemblyDebugging();
        // Enables the generation of Swagger documentation for your API
        webApplication.UseSwagger();
        // Middleware responsible for enabling Swagger UI,
        // which is a tool that generates interactive UI API documentation
        webApplication.UseSwaggerUI();
#else
        // Force all communication to be encrypted using HTTPS.
        // Server includes the Strict-Transport-Security header in the response, which tells the browser to always use HTTPS
        // Example header: Strict-Transport-Security: max-age=31536000; includeSubDomains
        // ; preload -> max-age=31536000 is the time in seconds that the browser should remember to use HTTPS, in this case 1 year
        // preload is a directive that tells the browser to include the site in the HSTS preload list,
        // which is a list of sites that are hardcoded into the browser to use HTTPS
        webApplication.UseHsts();
#endif
        // UseHttpsRedirection - Redirects all HTTP requests to HTTPS using a 301 (permanent) redirect
        webApplication.UseHttpsRedirection();
        // UseBlazorFrameworkFiles - Adds middleware that serves Blazor WebAssembly files
        // from the _framework directory like /_framework/blazor.webassembly.js
        webApplication.UseBlazorFrameworkFiles();
        // UseStaticFiles - This middleware enables the serving of static files, like HTML, CSS, images, and JavaScript
        webApplication.UseStaticFiles();
        // UseRouting - This middleware enables routing in your application. It's responsible for matching incoming HTTP requests
        // and passing those requests to the app's executable endpoints
        webApplication.UseRouting();
        // UseMiddleware - This middleware adds a custom MethodBlockingMiddleware middleware to your application
        // that blocks all requests that are not approved by a specific endpoint
        webApplication.UseMiddleware<MethodBlockingMiddleware>();
        // This middleware enables authentication in your application.
        // It sets the User property of the HttpContext with the appropriate ClaimsPrincipal.
        // ClaimsPrincipal is an object that represents the user's identity and contains the user's claims
        // Claims are key-value pairs that represent information about the user, like the user's name, email, or role
        webApplication.UseAuthentication();
        // UseAuthorization - This middleware enables authorization in your application.
        // It checks if the user is allowed to access the requested resource based on the user's claims and the authorization policies
        webApplication.UseAuthorization();
        // MapFallbackToPage - This middleware sets up a fallback route that's used when no other routes match.
        // It maps any requests that don't match any other routes to the _Host.cshtml Razor page
        webApplication.MapFallbackToPage("/_Host");
        // MapPrometheusScrapingEndpoint - This middleware maps an endpoint for Prometheus to scrape metrics from your application.
        // Prometheus is an open-source monitoring system and time series database
        webApplication.MapPrometheusScrapingEndpoint();
        // UseRateLimiter - This middleware applies rate limiting rules to your application.
        // It sets a limit on how many requests a client can make to the application within a certain timeframe
        // Apply a default limiter applied to all routes
        webApplication.UseRateLimiter();
        // UseSecurityHeadersPolicies - This middleware applies security-related headers to your application's responses. 
        // These headers are custom selected
        webApplication.UseSecurityHeadersPolicies();
        // MapControllers - This middleware maps the routes defined by your controllers.
        // This is necessary for the application to know which controller action to execute when a certain URL is requested
        webApplication.MapControllers();
        // This method starts the application and begins listening for incoming HTTP request
        webApplication.Run();
    }
}

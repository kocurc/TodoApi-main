using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Authorization;
using Todo.Web.Server.Database;
using Todo.Web.Server.Extensions;
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
        var databaseConnectionString = webApplicationBuilder.Configuration.GetConnectionString("SQLiteConnectionString") ?? "Data Source=.db/IEndpointRouteBuilderExtensions.db";

        webApplicationBuilder.Services.AddControllers();

        // ADD SERVICES TO THE APPLICATION
        // Configures who you are
        webApplicationBuilder.AddAuthentication();
        // Configures logging, distributed tracing and scraping metrics, for instance using Prometheus
        webApplicationBuilder.AddOpenTelemetry();
        // Adds service that can generate Swagger documents for your API. InferSecuritySchemes infers the security schemes from the authorization policies - JWT Bearer in this case
        webApplicationBuilder.Services.AddSwaggerGen(swaggerGenOptions => swaggerGenOptions.InferSecuritySchemes());
        // Configures what you can do
        webApplicationBuilder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();
        // Use SQLLite as the database
        webApplicationBuilder.Services.AddSqlite<TodoDbContext>(databaseConnectionString);
        // Add support for Razor C#-HTML pages
        webApplicationBuilder.Services.AddRazorPages();

        // ???
        webApplicationBuilder.Services.AddControllersWithViews();
        webApplicationBuilder.Services.AddServerSideBlazor();
        // ???

        // Adds per-user rate limiting to the application, with a limit of 100 requests every 10 seconds
        webApplicationBuilder.Services.AddRateLimiting();

        // ADD SINGLETON SERVICES. THEY CREATED ONCE PER APPLICATION AND EVERY REQUEST USES THE SAME INSTANCE
        // Add the service to generate JWT tokens
        webApplicationBuilder.Services.AddSingleton<ITokenService, TokenService>();
        // Enable API Explorer for OpenAPI documentation for the endpoints defined in your application using the Map methods, like MapPost in the IEndpointRouteBuilder interface
        webApplicationBuilder.Services.AddEndpointsApiExplorer();

        // AddIdentityCore - Add the core identity services to the Dependency Injection container that support UI login functionalities
        // AddEntityFrameworkStores - Adds Entity Framework stores for user data storing. TodoDbContext is DbContext that will interact with the database
        webApplicationBuilder.Services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<TodoDbContext>();

        // ADD SCOPED SERVICES. NEW INSTANCE IS CREATED FOR EVERY REQUEST
        // UserAuthenticationClient - Service that handles the user authentication Tasks
        webApplicationBuilder.Services.AddScoped<UserAuthenticationClient>();
        // CurrentUser - Service that holds the current user information
        webApplicationBuilder.Services.AddScoped<CurrentUser>();
        // ClaimsTransformation - Service that provides a way to apply custom logic to a user's claims after they have been authenticated
        webApplicationBuilder.Services.AddScoped<IClaimsTransformation, ClaimsTransformation>();
        // Registers IHttpClientFactory that handles HttpClient instances. HttpClient is used to send HTTP requests and receive HTTP responses
        webApplicationBuilder.Services.AddHttpClient();


        // ???
        webApplicationBuilder.Services.AddScoped<TodoService>();
        webApplicationBuilder.Services.AddScoped<AuthenticationService>();
        webApplicationBuilder.Services.AddScoped<UserService>();
        // ???

        // Build method creates a WebApplication object, which represents configured web application and its services. Later you can set up middleware - Use... and Map... methods to configure the application's request pipeline
        var webApplication = webApplicationBuilder.Build();

#if DEBUG
        // UseWebAssemblyDebugging - Adds middleware that enables debugging of Blazor WebAssembly application, like adding a breakpoint in the code or inspect variables
        webApplication.UseWebAssemblyDebugging();

        // ???
        webApplication.UseSwagger();
        webApplication.UseSwaggerUI();
#else
        webApplication.UseHsts();
#endif
        webApplication.UseHttpsRedirection();
        webApplication.UseBlazorFrameworkFiles();
        webApplication.UseStaticFiles();
        webApplication.UseRouting();
        webApplication.UseAuthentication();
        webApplication.UseAuthorization();
        webApplication.MapFallbackToPage("/_Host");
        webApplication.MapPrometheusScrapingEndpoint();
        webApplication.UseRateLimiter();
        webApplication.UseSecurityHeadersPolicies();
        webApplication.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
        webApplication.Run();
    }
}

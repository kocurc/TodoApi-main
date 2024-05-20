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
        // Adds service that can generate Swagger documents for your API. InferSecuritySchemes infers the security schemes from the authorization policies - JWT Bearer in this case
        webApplicationBuilder.AddSwaggerGen();
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
        // Enable API Explorer for OpenAPI documentation for the endpoints defined in your application using the Map methods, like MapPost in the IEndpointRouteBuilder interface
        webApplicationBuilder.Services.AddEndpointsApiExplorer();

        //----------------------------------------------------------
        webApplicationBuilder.Services.AddIdentityCore<TodoUser>().AddEntityFrameworkStores<TodoDbContext>();
        webApplicationBuilder.Services.AddCurrentUser();
        webApplicationBuilder.Services.AddHttpClient();
        webApplicationBuilder.Services.AddScoped<AuthClient>();

        var webApplication = webApplicationBuilder.Build();

#if DEBUG
        webApplication.UseWebAssemblyDebugging();
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
        webApplication.MapAuth();
        webApplication.MapTodos();
        webApplication.MapUsers();
        webApplication.UseRateLimiter();
        webApplication.UseSecurityHeadersPolicies();
        ////----------------------------------------------------------

        // Run the application
        webApplication.Run();
    }
}

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

public class Program_TodoApi
{
    private static void Main_TodoApi
        (string[] args)
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
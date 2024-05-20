using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Todo.Tests.Extensions;
using Todo.Web.Server;
using Todo.Web.Server.Authentication;
using Todo.Web.Server.Database;
using Todo.Web.Server.Users;
using Xunit;

namespace Todo.Tests.ApiTests;

public class TodoApplication : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _sqliteConnection = new("Filename=:memory:");

    public TodoDbContext CreateTodoDbContext()
    {
        var db = Services.GetRequiredService<IDbContextFactory<TodoDbContext>>().CreateDbContext();

        db.Database.EnsureCreated();

        return db;
    }

    public async Task CreateUserAsync(string username, string? password = null)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TodoUser>>();
        var newUser = new TodoUser { UserName = username };
        var result = await userManager.CreateAsync(newUser, password ?? Guid.NewGuid().ToString());

        Assert.True(result.Succeeded);
    }

    public HttpClient CreateClient(string id, bool isAdmin = false)
    {
        return CreateDefaultClient(new AuthHandler(req =>
        {
            var token = CreateToken(id, isAdmin);

            req.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        }));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _sqliteConnection.Open();
        builder.ConfigureServices(services =>
        {
            services.AddDbContextFactory<TodoDbContext>();
            services.AddDbContextOptions<TodoDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseSqlite(_sqliteConnection));
            services.Configure<IdentityOptions>(o =>
            {
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireDigit = false;
                o.Password.RequiredUniqueChars = 0;
                o.Password.RequiredLength = 1;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
            });
        });

        var keyBytes = new byte[32];
        var base64Key = Convert.ToBase64String(keyBytes);

        RandomNumberGenerator.Fill(keyBytes);
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Schemes:Bearer:SigningKeys:0:Issuer"] = "dotnet-user-jwts",
                ["Authentication:Schemes:Bearer:SigningKeys:0:Value"] = base64Key
            });
        });

        return base.CreateHost(builder);
    }

    private string CreateToken(string id, bool isAdmin = false)
    {
        var tokenService = Services.GetRequiredService<ITokenService>();

        return tokenService.GenerateToken(id, isAdmin);
    }

    protected override void Dispose(bool disposing)
    {
        _sqliteConnection.Dispose();
        base.Dispose(disposing);
    }


}

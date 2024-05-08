using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Todo.Web.Server.Authentication;

namespace Todo.Web.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure auth with the front end
        builder.AddAuthentication();
        builder.Services.AddAuthorizationBuilder();

        // Add razor pages so we can render the Blazor WASM todo component
        builder.Services.AddRazorPages();

        // Add the forwarder to make sending requests to the backend easier
        builder.Services.AddHttpForwarder();

        // Configure the HttpClient for the backend API
        var todoUrl = builder.Configuration.GetServiceUri("todoapi")?.ToString() ??
                      builder.Configuration["TodoApiUrl"] ??
                      throw new InvalidOperationException("Todo API URL is not configured");

        // Configure the HttpClient for the backend API
        builder.Services.AddHttpClient<AuthClient>(client =>
        {
            client.BaseAddress = new(todoUrl);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
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

        // TODO: Add security headers description in framework https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders
        app.UseSecurityHeaders(policies => policies
            .AddFrameOptionsDeny()
            .AddXssProtectionBlock()
            .AddContentTypeOptionsNoSniff()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(
                maxAgeInSeconds: 60 * 60 * 24 * 365) // maxage = one year in seconds
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .RemoveServerHeader()
            .AddContentSecurityPolicy(builder =>
            {
                builder.AddObjectSrc().None();
                builder.AddFormAction().Self();
                builder.AddFrameAncestors().None();
            })
            .AddCrossOriginOpenerPolicy(builder =>
            {
                builder.SameOrigin();
            })
            .AddCrossOriginEmbedderPolicy(builder =>
            {
                builder.RequireCorp();
            })
            .AddCrossOriginResourcePolicy(builder =>
            {
                builder.SameOrigin();
            })
            .AddCustomHeader("X-My-Test-Header", "Header value"));


        // Configure the APIs
        app.MapAuth();
        app.MapTodos(todoUrl);
        app.Run();
    }
}

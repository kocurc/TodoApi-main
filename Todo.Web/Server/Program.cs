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

        // Configure the APIs
        app.MapAuth();
        app.MapTodos(todoUrl);
        app.Run();
    }
}

using System;
using System.Collections.Generic;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Todo.Web.Server.Authentication;

namespace Todo.Web.Server.Extensions;

public static class WebApplicationBuilderExtensions
{
    private const string ExternalProviderKey = "ExternalProviderName";

    private delegate void ExternalAuthenticationProvider(AuthenticationBuilder authenticationBuilder, Action<object> configure);

    public static void SetExternalProvider(this AuthenticationProperties properties, string providerName)
    {
        properties.SetString(ExternalProviderKey, providerName);
    }

    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        // Our default scheme is cookies
        var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        // Add the default authentication cookie that will be used between the front end and
        // the backend.
        authenticationBuilder.AddCookie();

        // This is the cookie that will store the user information from the external login provider
        authenticationBuilder.AddCookie(AuthenticationSchemes.ExternalScheme);

        // Add JWT Bearer authentication
        authenticationBuilder.AddJwtBearer();

        // Add external auth providers based on configuration
        var externalProviders = new Dictionary<string, ExternalAuthenticationProvider>
        {
            ["GitHub"] = static (builder, configure) => builder.AddGitHub(configure),
            ["Google"] = static (builder, configure) => builder.AddGoogle(configure),
            ["Microsoft"] = static (builder, configure) => builder.AddMicrosoftAccount(configure),
            ["Auth0"] = static (builder, configure) => builder.AddAuth0WebAppAuthentication(configure)
                .WithAccessToken(configure),
        };

        foreach (var (providerName, provider) in externalProviders)
        {
            var section = builder.Configuration.GetSection($"Authentication:Schemes:{providerName}");

            if (section.Exists())
            {
                provider(authenticationBuilder, options =>
                {
                    // Bind this section to the specified options
                    section.Bind(options);

                    switch (options)
                    {
                        // This will save the information in the external cookie
                        case RemoteAuthenticationOptions remoteAuthenticationOptions:
                            remoteAuthenticationOptions.SignInScheme = AuthenticationSchemes.ExternalScheme;
                            break;
                        case Auth0WebAppOptions auth0WebAppOptions:
                            // Skip the cookie handler since we already add it
                            auth0WebAppOptions.SkipCookieMiddleware = true;
                            break;
                    }
                });

                if (providerName is "Auth0")
                {
                    // Set this up once
                    SetAuth0SignInScheme(builder);
                }
            }
        }

        // Add the service that resolves external providers so we can show them in the UI
        builder.Services.AddSingleton<ExternalProviders>();

        return builder;

        static void SetAuth0SignInScheme(WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme)
                .PostConfigure(o =>
                {
                    // The Auth0 APIs don't let you set the sign in scheme, it defaults to the default sign in scheme.
                    // Use named options to configure the underlying OpenIdConnectOptions's sign in scheme instead.
                    o.SignInScheme = AuthenticationSchemes.ExternalScheme;
                });
        }
    }

    /// <summary>
    /// Configures logging, distributed tracing, and metrics
    /// <list type="bullet">
    /// <item><term>Distributed tracing</term> uses the OTLP Exporter, which can be viewed with Jaeger</item>
    /// <item><term>Metrics</term> uses the Prometheus Exporter</item>
    /// <item><term>Logging</term> can use the OTLP Exporter, but due to limited vendor support it is not enabled by default</item>
    /// </list>
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName);
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resourceBuilder)
                    .AddOtlpExporter();
            });
        }

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                    .AddPrometheusExporter()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEventCountersInstrumentation(c =>
                    {
                        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters
                        c.AddEventSources(
                            "Microsoft.AspNetCore.Hosting",
                            "Microsoft-AspNetCore-Server-Kestrel",
                            "System.Net.Http",
                            "System.Net.Sockets",
                            "System.Net.NameResolution",
                            "System.Net.Security");
                    });
            })
            .WithTracing(tracing =>
            {
                // We need to use AlwaysSampler to record spans from Todo.Web.Server, because there it no OpenTelemetry instrumentation
                tracing.SetResourceBuilder(resourceBuilder)
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter();
                }
            });

        return builder;
    }
}

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
    private delegate void ExternalAuthenticationProvider(AuthenticationBuilder authenticationBuilder, Action<object> configure);

    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        authenticationBuilder.AddCookie();
        authenticationBuilder.AddCookie(AuthenticationSchemes.ExternalScheme);
        authenticationBuilder.AddJwtBearer();

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

            if (!section.Exists())
            {
                continue;
            }

            provider(authenticationBuilder, options =>
            {
                section.Bind(options);

                switch (options)
                {
                    case RemoteAuthenticationOptions remoteAuthenticationOptions:
                        remoteAuthenticationOptions.SignInScheme = AuthenticationSchemes.ExternalScheme;
                        break;
                    case Auth0WebAppOptions auth0WebAppOptions:
                        auth0WebAppOptions.SkipCookieMiddleware = true;
                        break;
                }
            });

            if (providerName is "Auth0")
            {
                SetAuth0SignInScheme(builder);
            }
        }

        builder.Services.AddSingleton<ExternalProviders>();

        return builder;

        static void SetAuth0SignInScheme(WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme)
                .PostConfigure(o =>
                {
                    o.SignInScheme = AuthenticationSchemes.ExternalScheme;
                });
        }
    }

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

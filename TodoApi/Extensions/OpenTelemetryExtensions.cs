﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TodoApi.Extensions;

public static class OpenTelemetryExtensions
{

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
                // We need to use AlwaysSampler to record spans
                // from Todo.Web.Server, because there it no OpenTelemetry
                // instrumentation
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

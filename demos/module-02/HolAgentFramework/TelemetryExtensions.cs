using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AgentFramework101;

/// <summary>
/// Extension methods for configuring telemetry with Agent Framework.
/// Agent Framework uses OpenTelemetry with GenAI Semantic Conventions.
/// See: https://learn.microsoft.com/en-us/agent-framework/user-guide/observability
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// The source name used for OpenTelemetry instrumentation.
    /// </summary>
    public const string SourceName = "AgentFramework101";

    /// <summary>
    /// Adds OpenTelemetry telemetry configuration for Agent Framework.
    /// </summary>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, string serviceName, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var useOtlpExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName);

        // Register TracerProvider factory
        services.AddSingleton<TracerProvider>(serviceProvider =>
        {
            var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource(SourceName)
                // Listen to the Microsoft.Extensions.AI source for chat client telemetry
                .AddSource("*Microsoft.Extensions.AI")
                // Listen to the Microsoft.Extensions.Agents source for agent telemetry
                .AddSource("*Microsoft.Extensions.Agents*");

            if (useOtlpExporter)
            {
                tracerProviderBuilder.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(otlpEndpoint!);
                });
            }
            else
            {
                tracerProviderBuilder.AddConsoleExporter();
            }

            return tracerProviderBuilder.Build();
        });

        // Register MeterProvider factory
        services.AddSingleton<MeterProvider>(serviceProvider =>
        {
            var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter(SourceName)
                // Agent Framework metrics
                .AddMeter("*Microsoft.Agents.AI");

            if (useOtlpExporter)
            {
                meterProviderBuilder.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(otlpEndpoint!);
                });
            }
            else
            {
                meterProviderBuilder.AddConsoleExporter();
            }

            return meterProviderBuilder.Build();
        });

        // Configure logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);

                if (useOtlpExporter)
                {
                    options.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = new Uri(otlpEndpoint!);
                    });
                }
                else
                {
                    options.AddConsoleExporter();
                }

                // Format log messages
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });
            
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }
}
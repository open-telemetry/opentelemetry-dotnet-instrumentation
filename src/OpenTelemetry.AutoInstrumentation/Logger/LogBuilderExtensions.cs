// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal static class LogBuilderExtensions
{
    private static Type? _loggingProviderSdkType;
    private static volatile bool _hostingStartupRan;

    // this method is only called from LoggingBuilderIntegration
    public static void AddOpenTelemetryLogsFromIntegration(ILoggingBuilder builder)
    {
        // For Net6, if HostingStartupAssembly is configured, we don't want to call integration again for host's ServiceCollection.
        // OpenTelemetryLogger-related services were already added to WebApplicationServiceCollection and will
        // be copied to host's ServiceCollection later. We can't depend on integration's
        // capability to detect if integration was called before (by checking if ServiceDescriptor with
        // given type is already added to ServiceCollection) as copying services from WebApplicationServiceCollection
        // to host's ServiceCollection happens AFTER integration is called.

        // All of this additional checking is NOT needed for net7. There we can rely on integration's capability
        // to detect if integration was called before, because WebApplicationServiceCollection is not used when building host.

        if (builder.Services is ServiceCollection && !(IsNet6() && _hostingStartupRan && IsHostServiceCollection(builder.Services)))
        {
            AddOpenTelemetryLogs(builder);
        }
    }

    // this method is only called from BootstrapperHostingStartup
    public static void AddOpenTelemetryLogsFromStartup(this ILoggingBuilder builder)
    {
        AddOpenTelemetryLogs(builder);
        _hostingStartupRan = true;
    }

    private static void AddOpenTelemetryLogs(ILoggingBuilder builder)
    {
        try
        {
            if (builder.Services == null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: The builder.Services property is not of the IServiceCollection type, so we're skipping the integration of logs with ServiceCollection.");
                return;
            }

            // Integrate AddOpenTelemetry only once for ServiceCollection.

            _loggingProviderSdkType ??= Type.GetType("OpenTelemetry.Logs.LoggerProviderBuilderSdk, OpenTelemetry");
            var openTelemetryLoggerProviderDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == _loggingProviderSdkType);
            if (openTelemetryLoggerProviderDescriptor != null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: AddOpenTelemetry already called on logging builder instance.");
                return;
            }

            var settings = Instrumentation.LogSettings.Value;
            var pluginManager = Instrumentation.PluginManager;

            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(Instrumentation.GeneralSettings.Value.EnabledResourceDetectors));

                options.IncludeFormattedMessage = settings.IncludeFormattedMessage;

                pluginManager?.ConfigureLogsOptions(options);

                if (settings.ConsoleExporterEnabled)
                {
                    if (pluginManager != null)
                    {
                        options.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
                    }
                }

                switch (settings.LogExporter)
                {
                    case LogExporter.Otlp:
                        options.AddOtlpExporter(otlpOptions =>
                        {
                            if (settings.OtlpExportProtocol.HasValue)
                            {
                                otlpOptions.Protocol = settings.OtlpExportProtocol.Value;
                            }

                            pluginManager?.ConfigureLogsOptions(otlpOptions);
                        });
                        break;
                    case LogExporter.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Logs exporter '{settings.LogExporter}' is incorrect");
                }
            });

            AutoInstrumentationEventSource.Log.Information($"Logs: Loaded AddOpenTelemetry from LoggingBuilder.");
        }
        catch (Exception ex)
        {
            AutoInstrumentationEventSource.Log.Error($"Error in AddOpenTelemetryLogs: {ex}");
            throw;
        }
    }

    private static bool IsNet6()
    {
        var frameworkDescription = FrameworkDescription.Instance;
        return frameworkDescription.Name == ".NET" && frameworkDescription.ProductVersion.StartsWith("6");
    }

    private static bool IsHostServiceCollection(IServiceCollection builderServices)
    {
        var applicationLifetimeType = Type.GetType("Microsoft.Extensions.Hosting.Internal.ApplicationLifetime, Microsoft.Extensions.Hosting");
        if (applicationLifetimeType == null)
        {
            return false;
        }

        var applicationLifetimeDescriptor = builderServices.FirstOrDefault(sd => sd.ImplementationType == applicationLifetimeType);
        return applicationLifetimeDescriptor != null;
    }
}
#endif

// <copyright file="LogBuilderExtensions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NET6_0_OR_GREATER

using System.Reflection;
using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal static class LogBuilderExtensions
{
    private static bool? _autoInstrumentationStartupAssemblyConfigured;
    private static Type? _loggingProviderSdkType;

    public static void AddOpenTelemetryLogsFromIntegration(this ILoggingBuilder builder)
    {
        // For Net6, if HostingStartupAssembly is configured, we don't want to call integration again for host's ServiceCollection.
        // OpenTelemetryLogger-related services were already added to WebApplicationServiceCollection and will
        // be copied to host's ServiceCollection later. We can't depend on integration's
        // capability to detect if integration was called before (by checking if ServiceDescriptor with
        // given type is already added to ServiceCollection) as copying services from WebApplicationServiceCollection
        // to host's ServiceCollection happens AFTER integration is called.

        // All of this additional checking is NOT needed for net7. There we can rely on integration's capability
        // to detect if integration was called before, because WebApplicationServiceCollection is not used when building host.

        if (builder.Services is ServiceCollection && !(IsNet6() && IsAutoInstrumentationStartupAssemblyConfigured() && IsHostServiceCollection(builder.Services)))
        {
            AddOpenTelemetryLogs(builder);
        }
    }

    public static ILoggingBuilder AddOpenTelemetryLogs(this ILoggingBuilder builder)
    {
        try
        {
            if (builder.Services == null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: The builder.Services property is not of the IServiceCollection type, so we're skipping the integration of logs with ServiceCollection.");
                return builder;
            }

            // Integrate AddOpenTelemetry only once for ServiceCollection.

            _loggingProviderSdkType ??= Type.GetType("OpenTelemetry.Logs.LoggerProviderBuilderSdk, OpenTelemetry");
            var openTelemetryLoggerProviderDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == _loggingProviderSdkType);
            if (openTelemetryLoggerProviderDescriptor != null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: AddOpenTelemetry already called on logging builder instance.");
                return builder;
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

        return builder;
    }

    private static bool IsAutoInstrumentationStartupAssemblyConfigured()
    {
        // If autoinstrumentation's HostingStartupAssembly is configured, assume integration was already called.
        _autoInstrumentationStartupAssemblyConfigured ??= IsAutoInstrumentationStartupAssemblySet();
        return _autoInstrumentationStartupAssemblyConfigured.Value;
    }

    private static bool IsNet6()
    {
        var frameworkDescription = FrameworkDescription.Instance;
        return frameworkDescription.Name == ".NET" && frameworkDescription.ProductVersion.StartsWith("6");
    }

    private static bool IsHostServiceCollection(IServiceCollection builderServices)
    {
        // check if assembly is loaded before trying to get type,
        // in order to avoid triggering an unnecessary load
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var extensionsHostingAssembly = assemblies.FirstOrDefault(a => IsExtensionsHostingAssembly(a));
        if (extensionsHostingAssembly == null)
        {
            return false;
        }

        var applicationLifetimeType = extensionsHostingAssembly.GetType("Microsoft.Extensions.Hosting.Internal.ApplicationLifetime");
        if (applicationLifetimeType == null)
        {
            return false;
        }

        var applicationLifetimeDescriptor = builderServices.FirstOrDefault(sd => sd.ImplementationType == applicationLifetimeType);
        return applicationLifetimeDescriptor != null;
    }

    private static bool IsExtensionsHostingAssembly(Assembly assembly)
    {
        const string expectedAssemblyName = "Microsoft.Extensions.Hosting";
        var assemblyName = assembly.FullName.AsSpan();
        if (assemblyName.Length <= expectedAssemblyName.Length)
        {
            return false;
        }

        return assemblyName.StartsWith(expectedAssemblyName.AsSpan()) && assemblyName[expectedAssemblyName.Length] == ',';
    }

    private static bool IsAutoInstrumentationStartupAssemblySet()
    {
        try
        {
            var environmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");
            return environmentVariable != null && environmentVariable.Contains("OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper", StringComparison.OrdinalIgnoreCase);
        }
        catch (SecurityException)
        {
            return false;
        }
    }
}
#endif

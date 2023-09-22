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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal static class LogBuilderExtensions
{
    private static Type? _loggingProviderSdkType;

    public static ILoggingBuilder AddOpenTelemetryLogs(this ILoggingBuilder builder)
    {
        try
        {
            if (!(builder.Services is IServiceCollection services))
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: The builder.Services property is not of the IServiceCollection type, so we're skipping the integration of logs with ServiceCollection.");
                return builder;
            }

            // Integrate AddOpenTelemetry only once for ServiceCollection.

            _loggingProviderSdkType ??= Type.GetType("OpenTelemetry.Logs.LoggerProviderBuilderSdk, OpenTelemetry");
            var openTelemetryLoggerProviderDescriptor = services.FirstOrDefault(descriptor => descriptor.ImplementationType == _loggingProviderSdkType);
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
}
#endif

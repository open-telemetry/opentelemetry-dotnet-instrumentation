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

#if !NETFRAMEWORK

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal static class LogBuilderExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogs(this ILoggingBuilder builder)
    {
        try
        {
            if (!(builder?.Services is ServiceCollection services))
            {
                return builder;
            }

            // Integrate AddOpenTelemetry only once for ServiceCollection.
            var openTelemetryLoggerProviderDescriptor = services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(OpenTelemetryLoggerProvider));
            if (openTelemetryLoggerProviderDescriptor != null)
            {
                return builder;
            }

            var settings = LogSettings.FromDefaultSources();
            builder.AddOpenTelemetry(options =>
            {
                if (settings.Plugins.Count > 0)
                {
                    options.InvokePlugins(settings.Plugins);
                }

                if (settings.ConsoleExporterEnabled)
                {
                    options.AddConsoleExporter();
                }

                switch (settings.LogExporter)
                {
                    case LogExporter.Otlp:
#if NETCOREAPP3_1
                        if (settings.Http2UnencryptedSupportEnabled)
                        {
                            // Adding the OtlpExporter creates a GrpcChannel.
                            // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                            // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                        }
#endif
                        options.AddOtlpExporter(options =>
                        {
                            if (settings.OtlpExportProtocol.HasValue)
                            {
                                options.Protocol = settings.OtlpExportProtocol.Value;
                            }
                        });
                        break;
                    case LogExporter.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Traces exporter '{settings.LogExporter}' is incorrect");
                }

                options.ParseStateValues = settings.ParseStateValues;
                options.IncludeFormattedMessage = settings.IncludeFormattedMessage;
            });

            AutoInstrumentationEventSource.Log.Trace($"Logs: Loaded AddOpenTelemetry from LoggingBuilder.");
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

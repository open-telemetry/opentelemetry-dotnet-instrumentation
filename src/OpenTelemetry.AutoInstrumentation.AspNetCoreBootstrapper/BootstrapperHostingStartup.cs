// <copyright file="BootstrapperHostingStartup.cs" company="OpenTelemetry Authors">
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

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Logs;

[assembly: HostingStartup(typeof(OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper.BootstrapperHostingStartup))]

namespace OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper;

/// <summary>
/// Add summary.
/// </summary>
public class BootstrapperHostingStartup : IHostingStartup
{
    private readonly LogSettings settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapperHostingStartup"/> class.
    /// </summary>
    public BootstrapperHostingStartup()
    {
        settings = LogSettings.FromDefaultSources();
    }

    /// <summary>
    /// This method gets called by the runtime to lightup ASP.NET Core OpenTelemetry Logs Collection.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    public void Configure(IWebHostBuilder builder)
    {
        try
        {
            builder.ConfigureLogging(logging => logging.AddOpenTelemetry(options =>
            {
                if (settings.LogPlugins.Count > 0)
                {
                    options.InvokePlugins(settings.LogPlugins);
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
            }));

            var applicationName = GetApplicationName();
            BootstrapperEventSource.Log.Trace($"BootstrapperHostingStartup loaded for application with name {applicationName}.");
        }
        catch (Exception ex)
        {
            BootstrapperEventSource.Log.Error($"Error in BootstrapperHostingStartup: {ex}");
            throw;
        }
    }

    private static string GetApplicationName()
    {
        try
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }
        catch (Exception ex)
        {
            BootstrapperEventSource.Log.Error($"Error getting AppDomain.CurrentDomain.FriendlyName: {ex}");
            return string.Empty;
        }
    }
}

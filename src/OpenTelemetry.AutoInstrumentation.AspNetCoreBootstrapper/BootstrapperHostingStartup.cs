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

using Microsoft.AspNetCore.Hosting;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logger;
using OpenTelemetry.AutoInstrumentation.Logging;

[assembly: HostingStartup(typeof(OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper.BootstrapperHostingStartup))]

namespace OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper;

/// <summary>
/// Add summary.
/// </summary>
internal class BootstrapperHostingStartup : IHostingStartup
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("AspNetCoreBootstrapper");

    private readonly LogSettings _logSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapperHostingStartup"/> class.
    /// </summary>
    public BootstrapperHostingStartup()
    {
        _logSettings = Instrumentation.LogSettings.Value;
    }

    /// <summary>
    /// This method gets called by the runtime to lightup ASP.NET Core OpenTelemetry Logs Collection.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    public void Configure(IWebHostBuilder builder)
    {
        if (!_logSettings.LogsEnabled)
        {
            Logger.Information("BootstrapperHostingStartup loaded, but OpenTelemetry Logs disabled. Skipping.");
            return;
        }

        if (!_logSettings.EnabledInstrumentations.Contains(LogInstrumentation.ILogger))
        {
            Logger.Information($"BootstrapperHostingStartup loaded, but {nameof(LogInstrumentation.ILogger)} instrumentation is disabled. Skipping.");
            return;
        }

        try
        {
            builder.ConfigureLogging(logging => logging.AddOpenTelemetryLogsFromStartup());

            var applicationName = GetApplicationName();
            Logger.Information($"BootstrapperHostingStartup loaded for application with name {applicationName}.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in BootstrapperHostingStartup: {ex}");
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
            Logger.Error($"Error getting AppDomain.CurrentDomain.FriendlyName: {ex}");
            return string.Empty;
        }
    }
}

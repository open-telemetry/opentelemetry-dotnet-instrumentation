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
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Logger;

[assembly: HostingStartup(typeof(OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper.BootstrapperHostingStartup))]

namespace OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper;

/// <summary>
/// Add summary.
/// </summary>
internal class BootstrapperHostingStartup : IHostingStartup
{
    private readonly LogSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapperHostingStartup"/> class.
    /// </summary>
    public BootstrapperHostingStartup()
    {
        _settings = LogSettings.FromDefaultSources();
    }

    /// <summary>
    /// This method gets called by the runtime to lightup ASP.NET Core OpenTelemetry Logs Collection.
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
    public void Configure(IWebHostBuilder builder)
    {
        if (!_settings.EnabledInstrumentations.Contains(LogInstrumentation.ILogger))
        {
            BootstrapperEventSource.Log.Trace($"BootstrapperHostingStartup loaded, but {nameof(LogInstrumentation.ILogger)} instrumentation is disabled. Skipping.");
            return;
        }

        try
        {
            builder.ConfigureLogging(logging => logging.AddOpenTelemetryLogs());

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

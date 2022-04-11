// <copyright file="OtelLogging.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.IO;
using OpenTelemetry.AutoInstrumentation.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Configures shared logger used by instrumentations.
/// </summary>
internal static class OtelLogging
{
    private const string NixDefaultDirectory = "/var/log/opentelemetry/dotnet";

    private static readonly ILogger Logger;

    static OtelLogging()
    {
        ISink sink = null;
        try
        {
            var logDirectory = GetLogDirectory();
            if (logDirectory != null)
            {
                var fileName = GetLogFileName();
                var logPath = Path.Combine(logDirectory, fileName);
                sink = new FileSink(logPath);
            }
        }
        catch (Exception)
        {
            // unable to configure logging to a file
        }

        if (sink == null)
        {
            sink = new NoopSink();
        }

        Logger = new Logger(sink);
    }

    internal static ILogger GetLogger() => Logger;

    private static string GetLogFileName()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var appDomainName = AppDomain.CurrentDomain.FriendlyName;

            return $"dotnet-tracer-managed-{appDomainName}-{process.Id}.log";
        }
        catch
        {
            // We can't get the process info
            return $"dotnet-tracer-managed-{Guid.NewGuid()}.log";
        }
    }

    private static string GetLogDirectory()
    {
        string logDirectory;

        try
        {
            logDirectory = Environment.GetEnvironmentVariable(ConfigurationKeys.LogDirectory);

            if (logDirectory == null)
            {
                var envVarValue = Environment.GetEnvironmentVariable(ConfigurationKeys.LogPath);

                if (!string.IsNullOrEmpty(envVarValue))
                {
                    logDirectory = Path.GetDirectoryName(envVarValue);
                }
            }

            if (logDirectory == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var windowsDefaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
                    logDirectory = windowsDefaultDirectory;
                }
                else
                {
                    // Linux
                    logDirectory = NixDefaultDirectory;
                }
            }

            logDirectory = CreateDirectoryIfMissing(logDirectory) ?? Path.GetTempPath();
        }
        catch
        {
            // The try block may throw a SecurityException if not granted the System.Security.Permissions.FileIOPermission
            // because of the following API calls
            //   - Directory.Exists
            //   - Environment.GetFolderPath
            //   - Path.GetTempPath

            // Unsafe to log
            logDirectory = null;
        }

        return logDirectory;
    }

    private static string CreateDirectoryIfMissing(string pathToCreate)
    {
        try
        {
            Directory.CreateDirectory(pathToCreate);
            return pathToCreate;
        }
        catch
        {
            // Unable to create the directory meaning that the user will have to create it on their own.
            // It is unsafe to log here, so return null to defer deciding what the path is
            return null;
        }
    }
}

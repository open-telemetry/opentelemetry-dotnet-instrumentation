// <copyright file="StartupLogger.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal static class StartupLogger
{
    private const string NixDefaultDirectory = "/var/log/opentelemetry/dotnet";

    private static readonly bool DebugEnabled = IsDebugEnabled();
    private static readonly string? LogDirectory = GetLogDirectory();
    private static readonly string? StartupLogFilePath = SetStartupLogFilePath();

    // It is not necessary to dispose of FileSink explicitly: the OS closes the respective
    // native handle when the process is closed. Moreover, this is a low-volume log and
    // each write operation is followed by a flush so no risk of losing some data on
    // intermediary buffers due to the lack of explicit dispose.
    private static readonly FileSink? LogFileSink = SetLogFileSink();

    public static void Log(string message, params object[] args)
    {
        try
        {
            if (LogFileSink != null)
            {
                try
                {
                    // It is possible a race with multiple threads under the same app domain.
                    lock (LogFileSink)
                    {
                        LogFileSink.Info($"[{DateTime.UtcNow:O}] [ThreadId: {Thread.CurrentThread.ManagedThreadId}] {message}{Environment.NewLine}", args);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Log: Exception writing to FileSink {ex}");
                }
            }

            Console.Error.WriteLine(message, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Log: Global exception: {ex}");
        }
    }

    public static void Log(Exception ex, string message, params object[] args)
    {
        message = $"{message}{Environment.NewLine}{ex}";
        Log(message, args);
    }

    public static void Debug(string message, params object[] args)
    {
        if (DebugEnabled)
        {
            Log(message, args);
        }
    }

    internal static string? SetStartupLogFilePath()
    {
        if (LogDirectory == null)
        {
            return null;
        }

        try
        {
            // Pick up the parts used to build the log file name and minimize the chances
            // of file name conflict with other processes.
            using var process = Process.GetCurrentProcess();

            // AppDomain friendly name can contain characters that are invalid in file names,
            // remove any of those. For the first assembly loaded by the process this is typically
            // expected to be name of the file with the application entry point.
            var appDomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
            HashSet<char> invalidChars = GetInvalidChars();
            var sb = new StringBuilder(appDomainFriendlyName);
            for (int i = 0; i < sb.Length; i++)
            {
                if (invalidChars.Contains(sb[i]))
                {
                    sb[i] = '_';
                }
            }

            appDomainFriendlyName = sb.ToString();

            // AppDomain friendly name may not be unique in the same process, use also the id.
            // Per documentation the id is an integer that uniquely identifies the application
            // domain within the process.
            var appDomainId = AppDomain.CurrentDomain.Id;

            return Path.Combine(LogDirectory, $"otel-dotnet-auto-loader-{appDomainFriendlyName}-{appDomainId}-{process?.Id}.log");
        }
        catch
        {
            // We can't get the process info
            return Path.Combine(LogDirectory, $"otel-dotnet-auto-loader-{Guid.NewGuid()}.log");
        }
    }

    private static HashSet<char> GetInvalidChars()
    {
        var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        foreach (var c in Path.GetInvalidPathChars())
        {
            invalidChars.Add(c);
        }

        return invalidChars;
    }

    private static string? GetLogDirectory()
    {
        string? logDirectory = null;

        try
        {
            logDirectory = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY");

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

    private static string? CreateDirectoryIfMissing(string pathToCreate)
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

    private static FileSink? SetLogFileSink()
    {
        if (StartupLogFilePath == null)
        {
            return null;
        }

        try
        {
            return new FileSink(StartupLogFilePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Log: Exception creating FileSink {ex}");
        }

        return null;
    }

    private static bool IsDebugEnabled()
    {
        try
        {
            var ddTraceDebugValue = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_DEBUG");

            if (ddTraceDebugValue == null)
            {
                return false;
            }

            switch (ddTraceDebugValue.ToUpperInvariant())
            {
                case "TRUE":
                case "YES":
                case "Y":
                case "T":
                case "1":
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            // Default to not enabled
            return false;
        }
    }
}

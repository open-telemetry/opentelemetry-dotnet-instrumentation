using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Datadog.Trace.Tools.Runner
{
    internal class Utils
    {
        public const string PROFILERID = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";

        public static Dictionary<string, string> GetProfilerEnvironmentVariables(string runnerFolder, Platform platform, Options options)
        {
            // In the current nuspec structure RunnerFolder has the following format:
            //  C:\Users\[user]\.dotnet\tools\.store\datadog.trace.tools.runner\[version]\datadog.trace.tools.runner\[version]\tools\netcoreapp3.1\any
            //  C:\Users\[user]\.dotnet\tools\.store\datadog.trace.tools.runner\[version]\datadog.trace.tools.runner\[version]\tools\netcoreapp2.1\any
            // And the Home folder is:
            //  C:\Users\[user]\.dotnet\tools\.store\datadog.trace.tools.runner\[version]\datadog.trace.tools.runner\[version]\home
            // So we have to go up 3 folders.
            string tracerHome = null;
            if (!string.IsNullOrEmpty(options.TracerHomeFolder))
            {
                tracerHome = options.TracerHomeFolder;
                if (!Directory.Exists(tracerHome))
                {
                    Console.Error.WriteLine("ERROR: The specified home folder doesn't exist.");
                }
            }

            tracerHome ??= DirectoryExists("Home", Path.Combine(runnerFolder, "..", "..", "..", "home"), Path.Combine(runnerFolder, "home"));
            string tracerMsBuild = FileExists(Path.Combine(tracerHome, "netstandard2.0", "OpenTelemetry.AutoInstrumentation.MSBuild.dll"));
            string tracerIntegrations = FileExists(Path.Combine(tracerHome, "integrations.json"));
            string tracerProfiler32 = string.Empty;
            string tracerProfiler64 = string.Empty;

            if (platform == Platform.Windows)
            {
                tracerProfiler32 = FileExists(Path.Combine(tracerHome, "win-x86", "OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"));
                tracerProfiler64 = FileExists(Path.Combine(tracerHome, "win-x64", "OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"));
            }
            else if (platform == Platform.Linux)
            {
                tracerProfiler64 = FileExists(Path.Combine(tracerHome, "linux-x64", "OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so"));
            }
            else if (platform == Platform.MacOS)
            {
                tracerProfiler64 = FileExists(Path.Combine(tracerHome, "osx-x64", "OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dylib"));
                if (RuntimeInformation.OSArchitecture == Architecture.X64 || RuntimeInformation.OSArchitecture == Architecture.X86)
                {
                    tracerProfiler32 = FileExists(Path.Combine(tracerHome, "win-x86", "OpenTelemetry.AutoInstrumentation.Native.dll"));
                    tracerProfiler64 = FileExists(Path.Combine(tracerHome, "win-x64", "OpenTelemetry.AutoInstrumentation.Native.dll"));
                }
                else
                {
                    Console.Error.WriteLine($"ERROR: Windows {RuntimeInformation.OSArchitecture} architecture is not supported.");
                    return null;
                }
            }
            else if (platform == Platform.Linux)
            {
                if (RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    tracerProfiler64 = FileExists(Path.Combine(tracerHome, "linux-x64", "OpenTelemetry.AutoInstrumentation.Native.so"));
                }
                else if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    tracerProfiler64 = FileExists(Path.Combine(tracerHome, "linux-arm64", "OpenTelemetry.AutoInstrumentation.Native.so"));
                }
                else
                {
                    Console.Error.WriteLine($"ERROR: Linux {RuntimeInformation.OSArchitecture} architecture is not supported.");
                    return null;
                }
            }
            else if (platform == Platform.MacOS)
            {
                if (RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    tracerProfiler64 = FileExists(Path.Combine(tracerHome, "osx-x64", "OpenTelemetry.AutoInstrumentation.Native.dylib"));
                }
                else
                {
                    Console.Error.WriteLine($"ERROR: macOS {RuntimeInformation.OSArchitecture} architecture is not supported.");
                    return null;
                }
            }

            var envVars = new Dictionary<string, string>
            {
                ["OTEL_DOTNET_TRACER_HOME"] = tracerHome,
                ["OTEL_DOTNET_TRACER_MSBUILD"] = tracerMsBuild,
                ["OTEL_INTEGRATIONS"] = tracerIntegrations,
                ["CORECLR_ENABLE_PROFILING"] = "1",
                ["CORECLR_PROFILER"] = PROFILERID,
                ["CORECLR_PROFILER_PATH_32"] = tracerProfiler32,
                ["CORECLR_PROFILER_PATH_64"] = tracerProfiler64,
                ["COR_ENABLE_PROFILING"] = "1",
                ["COR_PROFILER"] = PROFILERID,
                ["COR_PROFILER_PATH_32"] = tracerProfiler32,
                ["COR_PROFILER_PATH_64"] = tracerProfiler64,
            };

            if (!string.IsNullOrWhiteSpace(options.Environment))
            {
                envVars["OTEL_ENV"] = options.Environment;
            }

            if (!string.IsNullOrWhiteSpace(options.Service))
            {
                envVars["OTEL_SERVICE"] = options.Service;
            }

            if (!string.IsNullOrWhiteSpace(options.Version))
            {
                envVars["OTEL_VERSION"] = options.Version;
            }

            if (!string.IsNullOrWhiteSpace(options.AgentUrl))
            {
                envVars["OTEL_TRACE_AGENT_URL"] = options.AgentUrl;
            }

            if (!string.IsNullOrWhiteSpace(options.EnvironmentValues))
            {
                foreach (var keyValue in options.EnvironmentValues.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(keyValue?.Trim()))
                    {
                        var kvArray = keyValue.Split('=');
                        if (kvArray.Length == 2)
                        {
                            envVars[kvArray[0]] = kvArray[1];
                        }
                    }
                }
            }

            return envVars;
        }

        public static string DirectoryExists(string name, params string[] paths)
        {
            string folderName = null;

            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (Directory.Exists(paths[i]))
                    {
                        folderName = paths[i];
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: The '{name}' directory check thrown an exception: {ex}");
            }

            if (folderName == null)
            {
                Console.Error.WriteLine($"Error: The '{name}' directory can't be found.");
            }

            return folderName;
        }

        public static string FileExists(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine($"Error: The file '{filePath}' can't be found.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: The file '{filePath}' check thrown an exception: {ex}");
            }

            return filePath;
        }

        public static ProcessStartInfo GetProcessStartInfo(string filename, string currentDirectory, IDictionary<string, string> environmentVariables)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo(filename)
            {
                UseShellExecute = false,
                WorkingDirectory = currentDirectory,
            };

            IDictionary currentEnvVars = Environment.GetEnvironmentVariables();
            if (currentEnvVars != null)
            {
                foreach (DictionaryEntry item in currentEnvVars)
                {
                    processInfo.Environment[item.Key.ToString()] = item.Value.ToString();
                }
            }

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> item in environmentVariables)
                {
                    processInfo.Environment[item.Key] = item.Value;
                }
            }

            return processInfo;
        }

        public static int RunProcess(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            try
            {
                using (Process childProcess = new Process())
                {
                    childProcess.StartInfo = startInfo;
                    childProcess.EnableRaisingEvents = true;
                    childProcess.Start();

                    using (cancellationToken.Register(() =>
                    {
                        try
                        {
                            childProcess.Kill();
                        }
                        catch
                        {
                            // .
                        }
                    }))
                    {
                        childProcess.WaitForExit();
                        return cancellationToken.IsCancellationRequested ? 1 : childProcess.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return 1;
        }

        public static string[] SplitArgs(string command, bool keepQuote = false)
        {
            if (string.IsNullOrEmpty(command))
            {
                return new string[0];
            }

            var inQuote = false;
            var chars = command.ToCharArray().Select(v =>
            {
                if (v == '"')
                {
                    inQuote = !inQuote;
                }

                return !inQuote && v == ' ' ? '\n' : v;
            }).ToArray();

            return new string(chars).Split('\n')
                .Select(x => keepQuote ? x : x.Trim('"'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        public static string GetEnvironmentVariable(string key, string defaultValue = null)
        {
            try
            {
                return Environment.GetEnvironmentVariable(key);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error while reading environment variable {key}: {ex}");
            }

            return defaultValue;
        }
    }
}

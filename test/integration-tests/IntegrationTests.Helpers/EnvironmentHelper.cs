using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    public class EnvironmentHelper
    {
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private static readonly string RuntimeFrameworkDescription = RuntimeInformation.FrameworkDescription.ToLower();
        private static string _nukeOutputLocation;

        private readonly ITestOutputHelper _output;
        private readonly int _major;
        private readonly int _minor;
        private readonly string _patch = null;

        private readonly string _appNamePrepend;
        private readonly string _runtime;
        private readonly bool _isCoreClr;
        private readonly string _samplesDirectory;
        private readonly TargetFrameworkAttribute _targetFramework;

        private bool _requiresProfiling;
        private string _integrationsFileLocation;
        private string _profilerFileLocation;

        public EnvironmentHelper(
            string sampleName,
            Type anchorType,
            ITestOutputHelper output,
            string samplesDirectory = null,
            bool prependSamplesToAppName = true,
            bool requiresProfiling = true)
        {
            SampleName = sampleName;
            _samplesDirectory = samplesDirectory ?? Path.Combine("test", "test-applications", "integrations");
            _targetFramework = Assembly.GetAssembly(anchorType).GetCustomAttribute<TargetFrameworkAttribute>();
            _output = output;
            _requiresProfiling = requiresProfiling;

            var parts = _targetFramework.FrameworkName.Split(',');
            _runtime = parts[0];
            _isCoreClr = _runtime.Equals(EnvironmentTools.CoreFramework);

            var versionParts = parts[1].Replace("Version=v", string.Empty).Split('.');
            _major = int.Parse(versionParts[0]);
            _minor = int.Parse(versionParts[1]);

            if (versionParts.Length == 3)
            {
                _patch = versionParts[2];
            }

            _appNamePrepend = prependSamplesToAppName
                          ? "Samples."
                          : string.Empty;
        }

        public bool DebugModeEnabled { get; set; } = true;

        public Dictionary<string, string> CustomEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public string SampleName { get; }

        public string FullSampleName => $"{_appNamePrepend}{SampleName}";

        public static bool IsNet5()
        {
            return Environment.Version.Major >= 5;
        }

        public static bool IsCoreClr()
        {
            return RuntimeFrameworkDescription.Contains("core") || IsNet5();
        }

        public static void ClearProfilerEnvironmentVariables()
        {
            var environmentVariables = new[]
            {
                // .NET Core
                "CORECLR_ENABLE_PROFILING",
                "CORECLR_PROFILER",
                "CORECLR_PROFILER_PATH",
                "CORECLR_PROFILER_PATH_32",
                "CORECLR_PROFILER_PATH_64",
                "DOTNET_STARTUP_HOOKS",

                // .NET Framework
                "COR_ENABLE_PROFILING",
                "COR_PROFILER",
                "COR_PROFILER_PATH",

                // OpenTelemetry
                "OTEL_DOTNET_AUTO_INCLUDE_PROCESSES",
                "OTEL_DOTNET_AUTO_HOME",
                "OTEL_DOTNET_AUTO_INTEGRATIONS_FILE",
                "OTEL_DISABLED_INTEGRATIONS",
                "OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES",
                "OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES"
            };

            foreach (string variable in environmentVariables)
            {
                Environment.SetEnvironmentVariable(variable, null);
            }
        }

        public static string GetNukeBuildOutput()
        {
            string nukeOutputPath = Path.Combine(
                EnvironmentTools.GetSolutionDirectory(),
                "bin",
                "tracer-home");

            if (Directory.Exists(nukeOutputPath))
            {
                _nukeOutputLocation = nukeOutputPath;

                return _nukeOutputLocation;
            }

            throw new Exception($"Unable to find Nuke output at: {nukeOutputPath}. Ensure Nuke has run first.");
        }

        public static bool IsRunningOnCI()
        {
            // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
            // Github sets CI environment variable

            string env = Environment.GetEnvironmentVariable("CI");
            return !string.IsNullOrEmpty(env);
        }

        public void SetEnvironmentVariables(
            int agentPort,
            int aspNetCorePort,
            StringDictionary environmentVariables,
            bool enableStartupHook,
            string processToProfile = null)
        {
            string profilerEnabled = _requiresProfiling ? "1" : "0";
            string profilerPath = GetProfilerPath();

            if (IsCoreClr())
            {
                // enableStartupHook should be true by default, and the parameter should only be set
                // to false when testing the case that instrumentation should not be available.
                if (enableStartupHook)
                {
                    string hookPath = GetStartupHookOutputPath();
                    environmentVariables["DOTNET_STARTUP_HOOKS"] = hookPath;
                }

                environmentVariables["CORECLR_ENABLE_PROFILING"] = profilerEnabled;
                environmentVariables["CORECLR_PROFILER"] = EnvironmentTools.ProfilerClsId;
                environmentVariables["CORECLR_PROFILER_PATH"] = profilerPath;
            }
            else
            {
                environmentVariables["COR_ENABLE_PROFILING"] = profilerEnabled;
                environmentVariables["COR_PROFILER"] = EnvironmentTools.ProfilerClsId;
                environmentVariables["COR_PROFILER_PATH"] = profilerPath;
            }

            if (DebugModeEnabled)
            {
                environmentVariables["OTEL_TRACE_DEBUG"] = "1";
                environmentVariables["OTEL_DOTNET_AUTO_LOG_DIRECTORY"] = Path.Combine(EnvironmentTools.GetSolutionDirectory(), "build_data", "profiler-logs");
            }

            if (!string.IsNullOrEmpty(processToProfile))
            {
                environmentVariables["OTEL_DOTNET_AUTO_INCLUDE_PROCESSES"] = Path.GetFileName(processToProfile);
            }

            string integrations = GetIntegrationsPath();
            environmentVariables["OTEL_DOTNET_AUTO_HOME"] = GetNukeBuildOutput();
            environmentVariables["OTEL_DOTNET_AUTO_INTEGRATIONS_FILE"] = integrations;
            environmentVariables["OTEL_TRACES_EXPORTER"] = "zipkin";
            environmentVariables["OTEL_EXPORTER_ZIPKIN_ENDPOINT"] = $"http://127.0.0.1:{agentPort}";

            // for ASP.NET Core sample apps, set the server's port
            environmentVariables["ASPNETCORE_URLS"] = $"http://127.0.0.1:{aspNetCorePort}/";

            environmentVariables["OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES"] = "Samples.*";

            foreach (var key in CustomEnvironmentVariables.Keys)
            {
                environmentVariables[key] = CustomEnvironmentVariables[key];
            }
        }

        public string GetProfilerPath()
        {
            if (_profilerFileLocation != null)
            {
                return _profilerFileLocation;
            }

            string extension = EnvironmentTools.GetOS() switch
            {
                "win" => "dll",
                "linux" => "so",
                "osx" => "dylib",
                _ => throw new PlatformNotSupportedException()
            };

            string fileName = $"OpenTelemetry.ClrProfiler.Native.{extension}";
            string nukeOutput = GetNukeBuildOutput();
            string profilerPath = EnvironmentTools.IsWindows()
                ? Path.Combine(nukeOutput, $"win-{EnvironmentTools.GetPlatform().ToLower()}", fileName)
                : Path.Combine(nukeOutput, fileName);

            if (File.Exists(profilerPath))
            {
                _profilerFileLocation = profilerPath;
                _output?.WriteLine($"Found profiler at {_profilerFileLocation}.");
                return _profilerFileLocation;
            }

            throw new Exception($"Unable to find profiler at: {profilerPath}");
        }

        public string GetIntegrationsPath()
        {
            if (_integrationsFileLocation != null)
            {
                return _integrationsFileLocation;
            }

            string fileName = $"integrations.json";
            string integrationsPath = Path.Combine(GetNukeBuildOutput(), fileName);

            if (File.Exists(integrationsPath))
            {
                _integrationsFileLocation = integrationsPath;
                _output?.WriteLine($"Found integrations at {_profilerFileLocation}.");
                return _integrationsFileLocation;
            }

            throw new Exception($"Unable to find integrations at: {integrationsPath}");
        }

        public string GetSampleApplicationPath(string packageVersion = "", string framework = "")
        {
            string extension = "exe";

            if (IsCoreClr() || _samplesDirectory.Contains("aspnet"))
            {
                extension = "dll";
            }

            var appFileName = $"{FullSampleName}.{extension}";
            var sampleAppPath = Path.Combine(GetSampleApplicationOutputDirectory(packageVersion: packageVersion, framework: framework), appFileName);
            return sampleAppPath;
        }

        public string GetTestCommandForSampleApplicationPath(string packageVersion = "", string framework = "")
        {
            var appFileName = $"{FullSampleName}.dll";
            var sampleAppPath = Path.Combine(GetSampleApplicationOutputDirectory(packageVersion: packageVersion, framework: framework), appFileName);
            return sampleAppPath;
        }

        public string GetSampleExecutionSource()
        {
            string executor;

            if (_samplesDirectory.Contains("aspnet"))
            {
                executor = $"C:\\Program Files{(Environment.Is64BitProcess ? string.Empty : " (x86)")}\\IIS Express\\iisexpress.exe";
            }
            else if (IsCoreClr())
            {
                executor = EnvironmentTools.IsWindows() ? "dotnet.exe" : "dotnet";
            }
            else
            {
                var appFileName = $"{FullSampleName}.exe";
                executor = Path.Combine(GetSampleApplicationOutputDirectory(), appFileName);

                if (!File.Exists(executor))
                {
                    throw new Exception($"Unable to find executing assembly at {executor}");
                }
            }

            return executor;
        }

        public string GetDotNetTest()
        {
            if (EnvironmentTools.IsWindows())
            {
                if (!IsCoreClr())
                {
                    string filePattern = @"C:\Program Files (x86)\Microsoft Visual Studio\{0}\{1}\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
                    List<Tuple<string, string>> lstTuple = new List<Tuple<string, string>>
                    {
                        Tuple.Create("2019", "Enterprise"),
                        Tuple.Create("2019", "Professional"),
                        Tuple.Create("2019", "Community"),
                        Tuple.Create("2017", "Enterprise"),
                        Tuple.Create("2017", "Professional"),
                        Tuple.Create("2017", "Community"),
                    };

                    foreach (Tuple<string, string> tuple in lstTuple)
                    {
                        var tryPath = string.Format(filePattern, tuple.Item1, tuple.Item2);
                        if (File.Exists(tryPath))
                        {
                            return tryPath;
                        }
                    }
                }

                return "dotnet.exe";
            }

            return "dotnet";
        }

        public string GetSampleProjectDirectory()
        {
            var solutionDirectory = EnvironmentTools.GetSolutionDirectory();
            var projectDir = Path.Combine(
                solutionDirectory,
                _samplesDirectory,
                $"{FullSampleName}");
            return projectDir;
        }

        public string GetSampleApplicationOutputDirectory(string packageVersion = "", string framework = "")
        {
            var targetFramework = string.IsNullOrEmpty(framework) ? GetTargetFramework() : framework;
            var binDir = Path.Combine(
                GetSampleProjectDirectory(),
                "bin");

            if (_samplesDirectory.Contains("aspnet"))
            {
                return Path.Combine(
                    binDir,
                    EnvironmentTools.GetBuildConfiguration(),
                    "app.publish");
            }

            return Path.Combine(
                binDir,
                packageVersion,
                EnvironmentTools.GetPlatform().ToLowerInvariant(),
                EnvironmentTools.GetBuildConfiguration(),
                targetFramework);
        }

        public string GetTargetFramework()
        {
            if (_isCoreClr)
            {
                if (_major >= 5)
                {
                    return $"net{_major}.{_minor}";
                }

                return $"netcoreapp{_major}.{_minor}";
            }

            return $"net{_major}{_minor}{_patch ?? string.Empty}";
        }

        private static string GetStartupHookOutputPath()
        {
            string startupHookOutputPath = Path.Combine(
                GetNukeBuildOutput(),
                "netcoreapp3.1",
                "OpenTelemetry.Instrumentation.StartupHook.dll");

            return startupHookOutputPath;
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class EnvironmentHelper
{
    private static readonly string RuntimeFrameworkDescription = RuntimeInformation.FrameworkDescription.ToLower();

    private readonly ITestOutputHelper _output;
    private readonly int _major;
    private readonly int _minor;
    private readonly string? _patch = null;

    private readonly string _appNamePrepend;
    private readonly string _runtime;
    private readonly bool _isCoreClr;
    private readonly string _testApplicationDirectory;
    private readonly TargetFrameworkAttribute _targetFramework;

    private string? _profilerFileLocation;

    public EnvironmentHelper(
        string testApplicationName,
        Type anchorType,
        ITestOutputHelper output,
        string? testApplicationDirectory = null,
        string testApplicationType = "integrations",
        bool prependTestApplicationToAppName = true)
    {
        TestApplicationName = testApplicationName;
        _testApplicationDirectory = testApplicationDirectory ?? Path.Combine("test", "test-applications", testApplicationType);
        _targetFramework = Assembly.GetAssembly(anchorType)?.GetCustomAttribute<TargetFrameworkAttribute>()!;
        _output = output;

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

        _appNamePrepend = prependTestApplicationToAppName
            ? "TestApplication."
            : string.Empty;

        if (testApplicationType == "integrations")
        {
            // Only integration tests assume the default environment variable settings.
            SetDefaultEnvironmentVariables();
        }
    }

    public bool DebugModeEnabled { get; set; } = true;

    public Dictionary<string, string?> CustomEnvironmentVariables { get; set; } = new();

    public string TestApplicationName { get; }

    public string FullTestApplicationName => $"{_appNamePrepend}{TestApplicationName}";

    public static bool IsCoreClr()
    {
        return RuntimeFrameworkDescription.Contains("core") || Environment.Version.Major >= 5;
    }

    public static string GetNukeBuildOutput()
    {
        string nukeOutputPath = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "bin",
            "tracer-home");

        if (Directory.Exists(nukeOutputPath))
        {
            return nukeOutputPath;
        }

        throw new Exception($"Unable to find Nuke output at: {nukeOutputPath}. Ensure Nuke has run first.");
    }

    public static bool IsRunningOnCI()
    {
        // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
        // Github sets CI environment variable

        var env = Environment.GetEnvironmentVariable("CI");
        return !string.IsNullOrEmpty(env);
    }

    public void SetEnvironmentVariables(StringDictionary environmentVariables)
    {
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

        string fileName = $"OpenTelemetry.AutoInstrumentation.Native.{extension}";
        string nukeOutput = GetNukeBuildOutput();
        string profilerPath = Path.Combine(nukeOutput, EnvironmentTools.GetClrProfilerDirectoryName(), fileName);

        if (File.Exists(profilerPath))
        {
            _profilerFileLocation = profilerPath;
            _output?.WriteLine($"Found profiler at {_profilerFileLocation}.");
            return _profilerFileLocation;
        }

        throw new Exception($"Unable to find profiler at: {profilerPath}");
    }

    public string GetTestApplicationPath(string packageVersion = "", string framework = "", TestAppStartupMode startupMode = TestAppStartupMode.Auto)
    {
        var extension = startupMode switch
        {
            TestAppStartupMode.Auto => IsCoreClr() || _testApplicationDirectory.Contains("aspnet") ? ".dll" : GetExecutableExtension(),
            TestAppStartupMode.DotnetCLI => ".dll",
            TestAppStartupMode.Exe => GetExecutableExtension(),
            _ => throw new InvalidOperationException($"Unknown startup mode '{startupMode}'")
        };

        var appFileName = $"{FullTestApplicationName}{extension}";
        var testApplicationPath = Path.Combine(GetTestApplicationApplicationOutputDirectory(packageVersion: packageVersion, framework: framework), appFileName);
        return testApplicationPath;

        static string GetExecutableExtension()
        {
            return EnvironmentTools.IsWindows() ? ".exe" : string.Empty;
        }
    }

    public string GetTestApplicationExecutionSource()
    {
        string executor;

        if (_testApplicationDirectory.Contains("aspnet"))
        {
            executor = $"C:\\Program Files{(Environment.Is64BitProcess ? string.Empty : " (x86)")}\\IIS Express\\iisexpress.exe";
        }
        else if (IsCoreClr())
        {
            executor = EnvironmentTools.IsWindows() ? "dotnet.exe" : "dotnet";
        }
        else
        {
            var appFileName = $"{FullTestApplicationName}.exe";
            executor = Path.Combine(GetTestApplicationApplicationOutputDirectory(), appFileName);

            if (!File.Exists(executor))
            {
                throw new Exception($"Unable to find executing assembly at {executor}");
            }
        }

        return executor;
    }

    public string GetTestApplicationBaseBinDirectory()
    {
        var solutionDirectory = EnvironmentTools.GetSolutionDirectory();
        var projectDir = Path.Combine(
            solutionDirectory,
            _testApplicationDirectory,
            "bin",
            $"{FullTestApplicationName}");
        return projectDir;
    }

    public string GetTestApplicationApplicationOutputDirectory(string packageVersion = "", string framework = "")
    {
        var targetFramework = string.IsNullOrEmpty(framework) ? GetTargetFramework() : framework;
        var baseBinDirectory = GetTestApplicationBaseBinDirectory();

        if (_testApplicationDirectory.Contains("aspnet"))
        {
            return Path.Combine(
                baseBinDirectory,
                EnvironmentTools.GetBuildConfiguration(),
                "app.publish");
        }

        return Path.Combine(
            baseBinDirectory,
            packageVersion,
            EnvironmentTools.GetPlatformDir(),
            EnvironmentTools.GetBuildConfiguration(),
            targetFramework);
    }

    public string GetTargetFramework()
    {
        if (_isCoreClr)
        {
            return $"net{_major}.{_minor}";
        }

        return $"net{_major}{_minor}{_patch ?? string.Empty}";
    }

    private static string GetStartupHookOutputPath()
    {
        string startupHookOutputPath = Path.Combine(
            GetNukeBuildOutput(),
            "net",
            "OpenTelemetry.AutoInstrumentation.StartupHook.dll");

        return startupHookOutputPath;
    }

    private static string GetSharedStorePath()
    {
        string storePath = Path.Combine(
            GetNukeBuildOutput(),
            "store");

        return storePath;
    }

    private static string GetAdditionalDepsPath()
    {
        string additionalDeps = Path.Combine(
            GetNukeBuildOutput(),
            "AdditionalDeps");

        return additionalDeps;
    }

    private void SetDefaultEnvironmentVariables()
    {
        string profilerPath = GetProfilerPath();

        CustomEnvironmentVariables["DOTNET_STARTUP_HOOKS"] = GetStartupHookOutputPath();
        CustomEnvironmentVariables["DOTNET_SHARED_STORE"] = GetSharedStorePath();
        CustomEnvironmentVariables["DOTNET_ADDITIONAL_DEPS"] = GetAdditionalDepsPath();

        // call TestHelper.EnableBytecodeInstrumentation() to enable CoreCLR Profiler when bytecode instrumentation is needed
        // it is not enabled by default to make sure that the instrumentations that do not require CoreCLR Profiler are working without it
        CustomEnvironmentVariables["CORECLR_PROFILER"] = EnvironmentTools.ProfilerClsId;
        CustomEnvironmentVariables["CORECLR_PROFILER_PATH"] = profilerPath;

        CustomEnvironmentVariables["COR_ENABLE_PROFILING"] = "1";
        CustomEnvironmentVariables["COR_PROFILER"] = EnvironmentTools.ProfilerClsId;
        CustomEnvironmentVariables["COR_PROFILER_PATH"] = profilerPath;

        CustomEnvironmentVariables["OTEL_LOG_LEVEL"] = "debug";
        CustomEnvironmentVariables["OTEL_DOTNET_AUTO_LOG_DIRECTORY"] = Path.Combine(EnvironmentTools.GetSolutionDirectory(), "test-artifacts", "profiler-logs");
        CustomEnvironmentVariables["OTEL_DOTNET_AUTO_HOME"] = GetNukeBuildOutput();
        CustomEnvironmentVariables["OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES"] = "TestApplication.*";

        // exporters are disabled by default in order not to have errors in the logs
        CustomEnvironmentVariables["OTEL_TRACES_EXPORTER"] = "none";
        CustomEnvironmentVariables["OTEL_METRICS_EXPORTER"] = "none";
        CustomEnvironmentVariables["OTEL_LOGS_EXPORTER"] = "none";
    }
}

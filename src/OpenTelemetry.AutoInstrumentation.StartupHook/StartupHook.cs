// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    internal const string LoaderAssemblyName = "OpenTelemetry.AutoInstrumentation.Loader";
    internal const string LoaderTypeName = "OpenTelemetry.AutoInstrumentation.Loader.Loader";

    private const string StartuphookLoggerSuffix = "StartupHook";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger(StartuphookLoggerSuffix);

    // This property must be initialized before any rule is evaluated since it may be used during rule evaluation.
    internal static string? LoaderAssemblyLocation { get; set; }

    /// <summary>
    /// Load and initialize OpenTelemetry.AutoInstrumentation assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        _ = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.FailFast), out var failFast);
        var redirectEnabled = GetRedirectEnabled();

        try
        {
            LoaderAssemblyLocation = GetLoaderAssemblyLocation();

            var ruleEngine = new RuleEngine();
            if (!ruleEngine.ValidateRules())
            {
                throw new InvalidOperationException(
                    "Rule Engine Failure: One or more rules failed validation. Automatic Instrumentation won't be loaded.");
            }

            Logger.Information("Initialization.");

            if (IsStartupHookOnlyMode() && redirectEnabled)
            {
                // ASSEMBLY RESOLUTION STRATEGY
                //
                // === STARTUP HOOK ONLY ===
                // Without the native profiler's IL rewriting and NuGet's build-time resolution,
                // the customer application and its dependencies automatically load into the
                // Default ALC. The instrumentation cannot override versions there or prevent
                // customer code from loading first. To solve this, we hijack the application:
                // load its entry assembly into an isolated ALC alongside our dependencies,
                // execute customer entrypoint in the isolated ALC,
                // then exit to prevent the Default ALC copy from running.
                //
                // When the customer application loads into the isolated ALC, all its dependencies
                // trigger the isolated ALC's Load() method first (before Default ALC fallback).
                // This is our single control point for version resolution.
                // For each dependency, the isolated ALC compares the TPA version against the
                // instrumentation version and picks the higher one. Before loading, it validates
                // that the selected version >= the requested version. If the best available
                // version is still lower than requested, the isolated ALC skips the request
                // rather than loading an incompatible assembly.
                //
                // If isolation setup fails, the using statement reverts the context and control
                // returns to the .NET runtime, which falls back to normal execution (or fail-fast
                // if configured).

                Logger.Information("Starting isolated AssemblyLoadContext mode");
                GetTargetApp(out var targetAppPath, out var entryAssembly);
                using var resolver = new IsolatedAssemblyResolver(targetAppPath, entryAssembly, Logger);
                try
                {
                    var exitCode = resolver.Run();
                    Environment.Exit(exitCode);
                }
                catch (TargetInvocationException ex)
                {
                    Logger.Error(ex.InnerException ?? ex, "Target entrypoint threw exception");
                    Environment.Exit(-1);
                }

                // Other exceptions: the using statement cleans up and reverts to the Default ALC.
                // The exception then propagates to Initialize()'s catch block, where failFast controls the behavior:
                // - If failFast = true: exception is re-thrown (fail fast)
                // - If failFast = false: exception is suppressed, and control returns to the .NET runtime
                //   to execute the customer's entrypoint normally (graceful degradation)
            }

            // If we run normally with Native profiler, we load the Loader,
            // create an instance of OpenTelemetry.AutoInstrumentation.Loader.Loader
            // which will setup assembly resolution and initialize Instrumentation
            var loaderFilePath = Path.Combine(LoaderAssemblyLocation, $"{LoaderAssemblyName}.dll");
            var loaderAssembly = Assembly.LoadFrom(loaderFilePath)
                ?? throw new InvalidOperationException("Failed to load Loader assembly");
            var loaderInstance = loaderAssembly.CreateInstance(LoaderTypeName)
                ?? throw new InvalidOperationException("Failed to create an instance of the Loader");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error in StartupHook initialization: LoaderFolderLocation: {LoaderAssemblyLocation}");
            if (failFast)
            {
                throw;
            }
        }
        finally
        {
            OtelLogging.CloseLogger(StartuphookLoggerSuffix, Logger);
        }
    }

    private static bool IsStartupHookOnlyMode()
    {
        return Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.ProfilerEnabledVariable) != "1";
    }

    private static bool GetRedirectEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable(ConfigurationKeys.RedirectEnabled);
        if (bool.TryParse(envValue, out var redirectEnabled))
        {
            Logger.Information($"Redirect explicitly set via environment variable to: {redirectEnabled}");
            return redirectEnabled;
        }

        // Not explicitly set: default based on deployment type - true for standalone, false otherwise.
        // For standalone deployment we need to enable assembly redirection,
        // for non-standalone deployments, assembly resolution is handled at build time,
        // so isolation is not needed.
        redirectEnabled = DeploymentDetector.IsStandaloneDeployment();

        if (redirectEnabled)
        {
            Logger.Information("Detected standalone deployment. Redirect enabled by default.");
            return redirectEnabled;
        }

        Logger.Information("Detected non-standalone deployment (e.g., NuGet-based, assembly found in TPA). Redirect disabled by default.");
        return redirectEnabled;
    }

    private static void GetTargetApp(out string appPath, out Assembly? entryAssembly)
    {
        // Try entry assembly first (should already be loaded in Default ALC)
        entryAssembly = Assembly.GetEntryAssembly();
        if (!string.IsNullOrEmpty(entryAssembly?.Location))
        {
            appPath = entryAssembly.Location;
            return;
        }

        // Fallback: try command line args
        Logger.Warning("Entry assembly location is unavailable, falling back to command line parsing. This may indicate an unexpected runtime scenario.");

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 0 &&
            (args[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
             args[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(args[0]))
        {
            appPath = Path.GetFullPath(args[0]);
            return;
        }

        throw new InvalidOperationException(
            "Cannot determine target application path. " +
            "GetEntryAssembly().Location is empty and GetCommandLineArgs()[0] is not a valid assembly.");
    }

    private static string GetLoaderAssemblyLocation()
    {
        try
        {
            var startupAssemblyFilePath = Assembly.GetExecutingAssembly().Location;
            if (startupAssemblyFilePath.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                // This will only be used in case the local path exceeds max_path size limit
                startupAssemblyFilePath = startupAssemblyFilePath[4..];
            }

            // StartupHook and Loader assemblies are in the same path
            var startupAssemblyDirectoryPath = Path.GetDirectoryName(startupAssemblyFilePath)
                ?? throw new InvalidOperationException("StartupAssemblyFilePath is NULL");
            return startupAssemblyDirectoryPath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting loader directory location");
            throw;
        }
    }
}

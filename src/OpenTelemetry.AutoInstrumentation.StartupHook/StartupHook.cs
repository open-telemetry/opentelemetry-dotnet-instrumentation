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

            if (IsStartupHookOnlyMode())
            {
                // ASSEMBLY RESOLUTION STRATEGY
                //
                // === STARTUP HOOK ONLY ===
                // Lacks:
                //   - Native profiler's IL rewriting capabilities
                //   - Build-time version resolution (when deployed as NuGet package)
                // This means customer aseembly and its dependencies load to Default ALC automatically.
                // We cannot override versions in Default ALC or prevent customer dependencies from loading there.
                // Loading our dependencies to a separate custom ALC risks shared state drift
                // (e.g., ActivitySpan from DiagnosticSource).
                // Solution: Hijack customer application, load it into custom ALC, execute it there
                // alongside our dependencies, then exit to prevent runtime from re-executing it in Default ALC.
                // If setup fails, cleanup and let it run normally (or fail fast if configured).

                // ASSEMBLY RESOLUTION TIMING
                //
                // When customer assembly loads into a custom ALC, all its dependencies
                // go through the custom context's Load() method first (before Default ALC fallback).
                // This is our single control point for version resolution.

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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
#if NET
using System.Runtime.Loader;
#endif
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal class Loader
{
    private const string LoaderLoggerSuffix = "Loader";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger(LoaderLoggerSuffix);

    private static int _isExiting;

    /// <summary>
    /// Initializes static members of the <see cref="Loader"/> class.
    /// This method also attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    static Loader()
    {
#if NET
        // TODO 1. for PoC run the isolated ALC for StartupHook-only deployment here in Loader
        // TODO 1. but once we decide on whether we should avoid custom ALC effect on RuleEngine validations
        // TODO 1. we may want to move this to first thing in StartupHook to reduce assemblies leak to default ALC
        // TODO 2. Make sure that if isolation stays here we'll adapt OpenTelemetry.AutoInstrumentation.Loader.Tests.Ctor_LoadsManagedAssembly
        // TODO 2. to not hang because of the waitings dead-lock
        if (Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS") is string startupHooks &&
            startupHooks.Contains("OpenTelemetry.AutoInstrumentation.StartupHook", StringComparison.Ordinal) &&
            Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != "1")
        {
            RunInIsolation();
            return;
        }
#endif
        try
        {
            new AssemblyResolver(Logger).RegisterAssemblyResolving();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
        }

        TryLoadManagedAssembly();

        // AssemblyResolve_ManagedProfilerDependencies logs only if Debug enabled.
        // If Debug is not enabled, logger won't be needed anymore.
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            // Register shutdown on exit
            AppDomain.CurrentDomain.ProcessExit += OnExit;
        }
        else
        {
            OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);
        }
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
        {
            // OnExit() was already called before
            return;
        }

        OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);
    }

    private static void TryLoadManagedAssembly()
    {
        Logger.Information("Managed Loader TryLoadManagedAssembly()");

        try
        {
            var assembly = Assembly.Load("OpenTelemetry.AutoInstrumentation");
            if (assembly == null)
            {
                throw new FileNotFoundException("The assembly OpenTelemetry.AutoInstrumentation could not be loaded");
            }

            var type = assembly.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation", throwOnError: false);
            if (type == null)
            {
                throw new TypeLoadException("The type OpenTelemetry.AutoInstrumentation.Instrumentation could not be loaded");
            }

            var method = type.GetRuntimeMethod("Initialize", Type.EmptyTypes);
            if (method == null)
            {
                throw new MissingMethodException("The method OpenTelemetry.AutoInstrumentation.Instrumentation.Initialize could not be loaded");
            }

            method.Invoke(obj: null, parameters: null);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error when loading managed assemblies. {0}", ex.Message);
            throw;
        }
    }

#if NET
    private static void RunInIsolation()
    {
        // POC: StartupHook-only deployment with ALC isolation

        int? result = null;
        try
        {
            Logger.Information("Starting isolated ALC mode");

            var targetAppPath = GetTargetAppPath();
            var managedProfilerDirectory = ResolveManagedProfilerDirectory();

            Logger.Debug($"Target app path: {targetAppPath}");
            Logger.Debug($"Managed Profiler directory: {managedProfilerDirectory}");

            // 1. Create isolated context
            var ctx = new ManagedProfilerAssemblyLoadContext(managedProfilerDirectory);
            Logger.Debug("Created isolated AssemblyLoadContext");

            // 2. Enable contextual reflection (helps with Assembly.Load(string), Type.GetType)
            ctx.EnterContextualReflection();

            // 3. Load customer entry assembly into isolated context
            var targetEntryAssembly = ctx.LoadFromAssemblyPath(targetAppPath);
            Logger.Debug($"Loaded target entry assembly: {targetEntryAssembly.FullName}");

            // 4. Set entry assembly for those frameworks and third-party that depends on it

#if NET9_0_OR_GREATER
            Assembly.SetEntryAssembly(customerEntry);
            Logger.Debug("Set entry assembly via Public Api");
#else
            AssemblyHelper.SetEntryAssembly(targetEntryAssembly);
            Logger.Debug("Set entry assembly via reflection");
#endif

            // 5. Load and initialize OTel instrumentation in the SAME context
            var instrumentationPath = Path.Combine(managedProfilerDirectory, "OpenTelemetry.AutoInstrumentation.dll");
            var instrumentationAssembly = ctx.LoadFromAssemblyPath(instrumentationPath);
            Logger.Debug($"Loaded instrumentation assembly: {instrumentationAssembly.FullName}");

            var initType = instrumentationAssembly.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation");
            var initMethod = initType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
            initMethod?.Invoke(null, null);

            Logger.Information("Instrumentation initialized in isolated context");

            // 6. Invoke customer's Main
            var entryPoint = targetEntryAssembly.EntryPoint!;

            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var parameters = entryPoint.GetParameters().Length > 0 ? new object[] { args } : null;

            Logger.Information("Invoking target entrypoint");
            OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);

            result = entryPoint.Invoke(null, parameters) as int?;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to run target application in isolated mode");
            OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);
            result = -1;
            throw;
        }
        finally
        {
            // 7. Exit to prevent runtime from calling Main on Default ALC's copy
            Environment.Exit(result ?? 0);
        }
    }

    private static string GetTargetAppPath()
    {
        // Try command line args first
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 0 &&
            (args[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
             args[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        // Fallback: use entry assembly location (already loaded in Default ALC)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (!string.IsNullOrEmpty(entryAssembly?.Location))
        {
            return entryAssembly.Location;
        }

        throw new InvalidOperationException(
            "Cannot determine customer application path. " +
            "GetCommandLineArgs()[0] is not a valid assembly and GetEntryAssembly().Location is empty.");
    }

    private static string ResolveManagedProfilerDirectory()
    {
        string tracerFrameworkDirectory = "net";
        string tracerHomeDirectory = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;

        return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
    }
#endif
}

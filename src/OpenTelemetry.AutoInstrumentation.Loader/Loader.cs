// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
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
        // TODO: For Non-Standalone deployment (e.g. NuGet-based), we may skip AssemblyResolver or adjust it to
        // assemblies layout in application output without /net or /netfx subdirectories (if we decide to ship all those assemblies)
#if NET
        // For .Net (Core) if we run in isolated context (set up by StartupHook's IsolatedAssemblyLoadContext),
        // skip AssemblyResolver. The isolated AssemblyLoadContext already handles all assembly resolution.
        var currentContext = System.Runtime.Loader.AssemblyLoadContext.CurrentContextualReflectionContext;
        var isIsolated = currentContext?.Name == StartupHookConstants.IsolatedAssemblyLoadContextName;

        if (!isIsolated)
#endif
        {
            try
            {
                new AssemblyResolver(Logger).RegisterAssemblyResolving();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to register assembly resolving");
            }
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
}

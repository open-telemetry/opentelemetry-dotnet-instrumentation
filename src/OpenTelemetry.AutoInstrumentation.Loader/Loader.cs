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
    private static readonly IOtelLogger Logger = EnvironmentHelper.Logger;

    /// <summary>
    /// Initializes static members of the <see cref="Loader"/> class.
    /// This method also attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    static Loader()
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += new AssemblyResolver(Logger).AssemblyResolve_ManagedProfilerDependencies;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
        }

        TryLoadManagedAssembly();

        // AssemblyResolve_ManagedProfilerDependencies logs only if Debug enabled.
        // If Debug is not enabled, most probably it is normal to close this logger.
        // AppConfigUpdater still may use this logger, but in ASP.NET default AppDomain Loader will not be called.
        // For other application, AppConfigUpdater will be used only if app creates additional AppDomains - which is rare scenario.
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            EnvironmentHelper.CloseLogger();
        }
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

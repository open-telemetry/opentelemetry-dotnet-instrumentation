// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Abstract base for instrumentation initialization.
/// Provides the shared template: logger init → rule validation → mode-specific setup.
/// Subclasses implement mode-specific initialization and lifecycle hooks.
/// </summary>
internal abstract class InitializationSetup
{
    internal const string LoaderAssemblyName = "OpenTelemetry.AutoInstrumentation.Loader";
    internal const string LoaderTypeName = "OpenTelemetry.AutoInstrumentation.Loader.Loader";

    protected static readonly IOtelLogger Logger = OtelLogging.GetLogger(LoggerSuffix);

    private const string LoggerSuffix = "StartupHook";

    /// <summary>
    /// Gets the display name for this initialization mode (used in log messages).
    /// </summary>
    protected abstract string ModeName { get; }

    /// <summary>
    /// Template method that orchestrates the full initialization sequence.
    /// </summary>
    public void Run()
    {
        _ = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.FailFast), out var failFast);

        // all unhandled exceptions in StartupHook are terminal and should be handled where needed
        // https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md#error-handling-details
        try
        {
            Logger.Information($"{ModeName} Initialization.");

            var loaderAssemblyLocation = GetLoaderAssemblyLocation();

            Logger.Debug($"LoaderFolderLocation: {loaderAssemblyLocation}");

            var ruleEngine = new RuleEngine(loaderAssemblyLocation);
            if (!ruleEngine.ValidateRules())
            {
                throw new InvalidOperationException(
                    "Rule Engine Failure: One or more rules failed validation. Automatic Instrumentation won't be loaded.");
            }

            Initialize(loaderAssemblyLocation);
        }
        catch (Exception ex)
        {
            OnError(ex);
            Logger.Error(ex, $"Error in StartupHook initialization");
            if (failFast)
            {
                throw;
            }

            return;
        }
        finally
        {
            OtelLogging.CloseLogger(LoggerSuffix, Logger);
        }

        AfterInitialize();
    }

    /// <summary>
    /// Mode-specific initialization (load Loader, set up customer app, etc.).
    /// Called after rule validation succeeds.
    /// </summary>
    protected abstract void Initialize(string instrumentationHomePath);

    /// <summary>
    /// Called on error before logging and fail-fast handling.
    /// IsolatedSetup uses this to revert contextual reflection and entry assembly.
    /// </summary>
    protected virtual void OnError(Exception ex)
    {
    }

    /// <summary>
    /// Called after successful setup and logger cleanup.
    /// IsolatedSetup uses this to invoke the customer application entrypoint.
    /// </summary>
    protected virtual void AfterInitialize()
    {
    }

    /// <summary>
    /// Resolves the auto-instrumentation home directory.
    /// StartupHook and all agent assets (Loader, rule engine config, etc.) reside in this directory.
    /// </summary>
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
            return Path.GetDirectoryName(startupAssemblyFilePath)
                ?? throw new InvalidOperationException("StartupAssemblyFilePath is NULL");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting loader directory location");
            throw;
        }
    }
}

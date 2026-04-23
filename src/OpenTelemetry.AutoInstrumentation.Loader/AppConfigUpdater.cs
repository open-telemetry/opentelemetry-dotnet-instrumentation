// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// Handles update of config files for non-default AppDomain
/// </summary>
internal static class AppConfigUpdater
{
    private const string LoaderLoggerSuffix = "AppConfigUpdater";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger(LoaderLoggerSuffix);

    private static readonly Action<AppDomainSetup> Patch;
    private static int _isExiting;

    // we don't like beforefieldinit for this class,
    // we prefer it explicitly called only when ModifyConfig is called
#pragma warning disable CA1810
    static AppConfigUpdater()
#pragma warning restore CA1810
    {
        var mode = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_APP_DOMAIN_STRATEGY") ?? string.Empty;
        if (!Enum.TryParse<PatchMode>(mode, ignoreCase: true, out var patchMode))
        {
            patchMode = PatchMode.LoaderOptimizationSingleDomain;
        }

        Logger.Debug($"Use {patchMode} strategy for multiple app domains");

        switch (patchMode)
        {
            case PatchMode.LoaderOptimizationSingleDomain:
                Patch = appDomainSetup =>
                    appDomainSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
                break;
            case PatchMode.AssemblyRedirect:
                var updater = new AssemblyBindingUpdater(Logger, new AssemblyCatalog(Logger));
                Patch = updater.ModifyAssemblyRedirectConfig;
                break;
            default:
                Patch = setup => { };
                break;
        }

        AppDomain.CurrentDomain.ProcessExit += OnExit;
    }

    private enum PatchMode
    {
        LoaderOptimizationSingleDomain,
        AssemblyRedirect,
        None
    }

    /// <summary>
    /// Modify assembly bindings in appDomainSetup
    /// </summary>
    /// <param name="appDomainSetup">appDomainSetup to be updated</param>
    public static void ModifyConfig(AppDomainSetup appDomainSetup)
        => Patch(appDomainSetup);

    private static void OnExit(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
        {
            // OnExit() was already called before
            return;
        }

        OtelLogging.CloseLogger(LoaderLoggerSuffix, Logger);
    }
}

#endif

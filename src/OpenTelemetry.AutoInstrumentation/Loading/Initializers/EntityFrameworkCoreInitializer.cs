// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class EntityFrameworkCoreInitializer : InstrumentationInitializer
{
    private const string NpgsqlEfCoreProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public EntityFrameworkCoreInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
        : base("Microsoft.EntityFrameworkCore", nameof(EntityFrameworkCoreInitializer))
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentation, OpenTelemetry.Instrumentation.EntityFrameworkCore")!;

        var options = new OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions();

        _pluginManager.ConfigureTracesOptions(options);
        ConfigureNpgsqlSuppressionIfNeeded(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }

    private void ConfigureNpgsqlSuppressionIfNeeded(OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions options)
    {
        if (!_tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.Npgsql))
        {
            return;
        }

        var currentFilter = options.Filter;
        options.Filter = (providerName, command) =>
            !string.Equals(providerName, NpgsqlEfCoreProviderName, StringComparison.Ordinal) &&
            (currentFilter?.Invoke(providerName, command) ?? true);

        Logger.Information("Configured EntityFrameworkCore instrumentation to skip Npgsql provider because Npgsql instrumentation is enabled.");
    }
}
#endif

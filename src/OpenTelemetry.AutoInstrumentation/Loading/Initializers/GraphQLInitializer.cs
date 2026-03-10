// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class GraphQLInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public GraphQLInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
    : base("GraphQL", nameof(GraphQLInitializer))
    {
        _pluginManager = pluginManager;
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var initializerType = Type.GetType("OpenTelemetry.AutoInstrumentation.Initializer, GraphQL");
        if (initializerType == null)
        {
            return;
        }

        var optionsType = Type.GetType("GraphQL.Telemetry.GraphQLTelemetryOptions, GraphQL")!;
        var optionsInstance = Activator.CreateInstance(optionsType)!;

        optionsType?.GetProperty("RecordDocument")?
            .SetValue(optionsInstance, _tracerSettings.InstrumentationOptions.GraphQLSetDocument);

        _pluginManager.ConfigureTracesOptions(optionsInstance);

        initializerType.GetMethod("EnableAutoInstrumentation", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [optionsInstance]);
    }
}
#endif

// <copyright file="GraphQLInitializer.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class GraphQLInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;
    private readonly TracerSettings _tracerSettings;

    public GraphQLInitializer(PluginManager pluginManager, TracerSettings tracerSettings)
    : base("GraphQL")
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
            .Invoke(null, new[] { optionsInstance });
    }
}

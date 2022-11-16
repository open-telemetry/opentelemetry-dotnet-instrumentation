// <copyright file="SqlClientInitializer.cs" company="OpenTelemetry Authors">
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

using System;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class SqlClientInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public SqlClientInitializer(PluginManager pluginManager)
        : base("Microsoft.Data.SqlClient")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentation, OpenTelemetry.Instrumentation.SqlClient");

        var options = new OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions();
        _pluginManager.ConfigureOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options);

        lifespanManager.Track(instrumentation);
    }
}

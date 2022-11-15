// <copyright file="WcfInitializer.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class WcfInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public WcfInitializer(PluginManager pluginManager)
        : base("System.ServiceModel.Primitives")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var options = new WcfInstrumentationOptions();

        _pluginManager.ConfigureOptions(options);

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationActivitySource, OpenTelemetry.Instrumentation.Wcf");

        instrumentationType.GetProperty("Options")?.SetValue(null, options);
    }
}

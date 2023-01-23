// <copyright file="GrpcClientInitializer.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class GrpcClientInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public GrpcClientInitializer(PluginManager pluginManager)
        : base("Grpc.Net.Client")
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentation, OpenTelemetry.Instrumentation.GrpcNetClient")!;

        var options = new OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentationOptions
        {
            SuppressDownstreamInstrumentation = !Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(TracerInstrumentation.HttpClient)
        };

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentation = Activator.CreateInstance(instrumentationType, options)!;

        lifespanManager.Track(instrumentation);
    }
}

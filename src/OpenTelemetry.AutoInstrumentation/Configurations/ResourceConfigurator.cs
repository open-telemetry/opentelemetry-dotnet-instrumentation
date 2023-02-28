// <copyright file="ResourceConfigurator.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ResourceConfigurator
{
    public static void Configure(ResourceBuilder resourceBuilder)
    {
        var pluginManager = Instrumentation.PluginManager;

        resourceBuilder
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector()
            .AddServiceNameDetector()
            .AddAttributes(new KeyValuePair<string, object>[] { new(Constants.Tracer.AutoInstrumentationVersionName, Constants.Tracer.Version) });

        if (pluginManager != null)
        {
            resourceBuilder.InvokePlugins(pluginManager);
        }
    }

    public static ResourceBuilder CreateResourceBuilder()
    {
        var resourceBuilder = ResourceBuilder.CreateDefault();
        Configure(resourceBuilder);
        return resourceBuilder;
    }
}

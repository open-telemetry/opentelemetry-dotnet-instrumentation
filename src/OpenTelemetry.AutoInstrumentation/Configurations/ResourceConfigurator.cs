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
    public static ResourceBuilder CreateResourceBuilder()
    {
        var resourceBuilder = ResourceBuilder
            .CreateEmpty() // Don't use CreateDefault because it puts service name unknown by default.
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk()
            .AddAttributes(new KeyValuePair<string, object>[] { new(Constants.Tracer.AutoInstrumentationVersionName, Constants.Tracer.Version) });

        var pluginManager = Instrumentation.PluginManager;
        if (pluginManager != null)
        {
            resourceBuilder.InvokePlugins(pluginManager);
        }

        var resource = resourceBuilder.Build();
        var serviceName = resource.Attributes.FirstOrDefault(a => a.Key == "service.name").Value as string;
        if (serviceName?.StartsWith("unknown_service:") == true)
        {
            // Fallback service name
            resourceBuilder.AddAttributes(new KeyValuePair<string, object>[] { new("service.name", ServiceNameConfigurator.GetFallbackServiceName()) });
        }

        return resourceBuilder;
    }
}

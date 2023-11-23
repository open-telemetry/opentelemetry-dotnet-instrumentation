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

using System.Runtime.CompilerServices;
using OpenTelemetry.ResourceDetectors.Azure;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ResourceConfigurator
{
    internal const string ServiceNameAttribute = "service.name";

    public static ResourceBuilder CreateResourceBuilder(IReadOnlyList<ResourceDetector> enabledResourceDetectors)
    {
        var resourceBuilder = ResourceBuilder
            .CreateEmpty() // Don't use CreateDefault because it puts service name unknown by default.
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk()
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new(Constants.DistributionAttributes.TelemetryDistroNameAttributeName, Constants.DistributionAttributes.TelemetryDistroNameAttributeValue),
                new(Constants.DistributionAttributes.TelemetryDistroVersionAttributeName, AutoInstrumentationVersion.Version)
            });

        foreach (var enabledResourceDetector in enabledResourceDetectors)
        {
            resourceBuilder = enabledResourceDetector switch
            {
                ResourceDetector.Container => Wrappers.AddContainerResourceDetector(resourceBuilder),
                ResourceDetector.AzureAppService => Wrappers.AddAzureAppServiceResourceDetector(resourceBuilder),
                _ => resourceBuilder
            };
        }

        var pluginManager = Instrumentation.PluginManager;
        if (pluginManager != null)
        {
            resourceBuilder.InvokePlugins(pluginManager);
        }

        var resource = resourceBuilder.Build();
        if (!resource.Attributes.Any(kvp => kvp.Key == ServiceNameAttribute))
        {
            // service.name was not configured yet use the fallback.
            resourceBuilder.AddAttributes(new KeyValuePair<string, object>[] { new(ServiceNameAttribute, ServiceNameConfigurator.GetFallbackServiceName()) });
        }

        return resourceBuilder;
    }

    private static class Wrappers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddContainerResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddDetector(new ContainerResourceDetector());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddAzureAppServiceResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddDetector(new AppServiceResourceDetector());
        }
    }
}

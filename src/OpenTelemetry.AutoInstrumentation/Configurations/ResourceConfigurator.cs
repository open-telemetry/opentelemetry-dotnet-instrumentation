// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ResourceConfigurator
{
    public static ResourceBuilder CreateResourceBuilder(IReadOnlyList<ResourceDetector> enabledResourceDetectors, IReadOnlyList<KeyValuePair<string, object>> resources)
    {
        var resourceBuilder = ResourceBuilder
            .CreateEmpty() // Don't use CreateDefault because it puts service name unknown by default.
            .AddEnvironmentVariableDetector()
            .AddTelemetrySdk()
            .AddAttributes(resources);

        foreach (var enabledResourceDetector in enabledResourceDetectors)
        {
            resourceBuilder = enabledResourceDetector switch
            {
#if NET
                ResourceDetector.Container => Wrappers.AddContainerResourceDetector(resourceBuilder),
#endif
                ResourceDetector.AzureAppService => Wrappers.AddAzureAppServiceResourceDetector(resourceBuilder),
                ResourceDetector.ProcessRuntime => Wrappers.AddProcessRuntimeResourceDetector(resourceBuilder),
                ResourceDetector.Process => Wrappers.AddProcessResourceDetector(resourceBuilder),
                ResourceDetector.Host => Wrappers.AddHostResourceDetector(resourceBuilder),
                ResourceDetector.OperatingSystem => Wrappers.AddOperatingSystemResourceDetector(resourceBuilder),
                _ => resourceBuilder
            };
        }

        var resource = resourceBuilder.Build();
        if (!resource.Attributes.Any(kvp => kvp.Key == Constants.ResourceAttributes.AttributeServiceName))
        {
            // service.name was not configured yet use the fallback.
            resourceBuilder.AddAttributes([new(Constants.ResourceAttributes.AttributeServiceName, ServiceNameConfigurator.GetFallbackServiceName())]);
        }

        var pluginManager = Instrumentation.PluginManager;
        if (pluginManager != null)
        {
            resourceBuilder.InvokePlugins(pluginManager);
        }

        return resourceBuilder;
    }

    private static class Wrappers
    {
#if NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddContainerResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddContainerDetector();
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddAzureAppServiceResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddAzureAppServiceDetector();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddProcessRuntimeResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddProcessRuntimeDetector();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddProcessResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddProcessDetector();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddOperatingSystemResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddOperatingSystemDetector();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ResourceBuilder AddHostResourceDetector(ResourceBuilder resourceBuilder)
        {
            return resourceBuilder.AddHostDetector();
        }
    }
}

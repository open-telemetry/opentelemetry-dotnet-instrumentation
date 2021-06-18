using System;
using System.Collections.Generic;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    internal static class PluginsConfigurationHelper
    {
        public static TracerProviderBuilder InvokePlugins(this TracerProviderBuilder builder, IEnumerable<string> pluginsAssemblyQualifiedNames)
        {
            foreach (var assemblyQualifiedName in pluginsAssemblyQualifiedNames)
            {
                builder = builder.InvokePlugin(assemblyQualifiedName);
            }

            return builder;
        }

        private static TracerProviderBuilder InvokePlugin(this TracerProviderBuilder builder, string pluginAssemblyQualifiedName)
        {
            // get the type and method
            var t = Type.GetType(pluginAssemblyQualifiedName);
            var mi = t.GetMethod("ConfigureTracerProvider", new Type[] { typeof(TracerProviderBuilder) });

            // execute
            var obj = Activator.CreateInstance(t);
            var result = mi.Invoke(obj, new object[] { builder });
            return (TracerProviderBuilder)result;
        }
    }
}

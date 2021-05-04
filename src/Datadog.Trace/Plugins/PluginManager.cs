using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Newtonsoft.Json.Linq;

namespace Datadog.Trace.Plugins
{
    internal static class PluginManager
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(PluginManager));

        internal static IReadOnlyCollection<IOTelPlugin> TryLoadPlugins(JsonConfigurationSource pluginsConfig)
        {
            if (pluginsConfig == null)
            {
                return ArrayHelper.Empty<IOTelPlugin>();
            }

            var runtimeName = GetPluginsRuntimeName();

            Log.Debug("Executing plugins configuration: {0}", pluginsConfig);
            Log.Information("Trying to load plugins for '{0}' runtime.", runtimeName);

            // TODO: Detect if objects instead of path
            var pluginFiles = pluginsConfig?.GetValue<JToken>($"['{runtimeName}']").ToObject<string[]>();

            if (pluginFiles == null || !pluginFiles.Any())
            {
                Log.Information("Skipping plugins load. Could not find any plugins for '{0}' runtime.", runtimeName);

                return ArrayHelper.Empty<IOTelPlugin>();
            }

            var loadedPlugins = TryLoadPlugins(pluginFiles);

            Log.Information("Successfully loaded '{0}' plugin(s).", property: loadedPlugins.Count);

            return loadedPlugins;
        }

        private static IReadOnlyCollection<IOTelPlugin> TryLoadPlugins(string[] pluginFiles)
        {
            var loaded = new List<IOTelPlugin>();

            foreach (string file in pluginFiles)
            {
                string fullPath = Path.GetFullPath(file);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        Assembly pluginAssembly = Assembly.LoadFrom(fullPath);
                        IOTelPlugin plugin = ConvertToPlugin(pluginAssembly);

                        if (plugin != null)
                        {
                            loaded.Add(plugin);

                            Log.Information("Plugin assembly loaded '{0}'.", pluginAssembly.FullName);
                        }
                        else
                        {
                            Log.Warning("Could not load {0} from '{1}'.", nameof(IOTelPlugin), pluginAssembly.FullName);
                        }
                    }
                    catch (Exception ex) when (
                        ExceptionUtil.IsAssemblyLoadException(ex) ||
                        ExceptionUtil.IsDynamicInvocationException(ex))
                    {
                        Log.Warning(ex, "Plugin assembly could not be loaded. Skipping vendor plugin load.");
                    }
                }
                else
                {
                    Log.Warning("Plugin path is defined but could not find the path '{0}'.", fullPath);
                }
            }

            return loaded;
        }

        private static IOTelPlugin ConvertToPlugin(Assembly assembly)
        {
            var pluginType = typeof(IOTelPlugin);

            var pluginTypes = assembly
                .GetTypes()
                .Where(p => pluginType.IsAssignableFrom(p))
                .ToList();

            var pluginInstance = pluginTypes
                .Select(p => (IOTelPlugin)Activator.CreateInstance(p))
                .FirstOrDefault();

            if (pluginTypes.Count > 1)
            {
                Log.Warning("Detected {0} plugins in the assembly '{1}'. Loading only the first type '{2}'.", pluginTypes.Count, assembly.FullName, pluginInstance.GetType().FullName);
            }

            return pluginInstance;
        }

        private static string GetPluginsRuntimeName()
        {
            // returns the runtime directory of the current running tracer
            return Directory.GetParent(typeof(PluginManager).Assembly.Location).Name;
        }
    }
}

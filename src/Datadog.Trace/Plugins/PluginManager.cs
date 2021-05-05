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

        internal static IReadOnlyCollection<IOTelExtension> TryLoadPlugins(JsonConfigurationSource pluginsConfig)
        {
            if (pluginsConfig == null)
            {
                return ArrayHelper.Empty<IOTelExtension>();
            }

            var targetFramework = FrameworkDescription.Instance.TargetFramework;

            Log.Debug("Executing plugins configuration: {0}", pluginsConfig);
            Log.Information("Trying to load plugins with target framework '{0}'.", targetFramework);

            // TODO: Detect if objects instead of path
            var pluginFiles = pluginsConfig.GetValue<JToken>($"['{targetFramework}']").ToObject<string[]>();

            if (pluginFiles == null || !pluginFiles.Any())
            {
                Log.Information("Skipping plugins load. Could not find any plugins with target framework '{0}'.", targetFramework);

                return ArrayHelper.Empty<IOTelExtension>();
            }

            var loadedPlugins = TryLoadPlugins(pluginFiles);

            Log.Information("Successfully loaded '{0}' plugin(s).", property: loadedPlugins.Count);

            return loadedPlugins;
        }

        private static IReadOnlyCollection<IOTelExtension> TryLoadPlugins(string[] pluginFiles)
        {
            var loaded = new List<IOTelExtension>();

            foreach (string file in pluginFiles)
            {
                string fullPath = Path.GetFullPath(file);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        Assembly pluginAssembly = Assembly.LoadFrom(fullPath);
                        ICollection<IOTelExtension> extensions = GetExtensions(pluginAssembly);

                        if (extensions != null && extensions.Any())
                        {
                            loaded.AddRange(extensions);

                            Log.Information("Plugin assembly loaded '{0}'.", pluginAssembly.FullName);
                        }
                        else
                        {
                            Log.Warning("Could not load {0} from '{1}'.", nameof(IOTelExtension), pluginAssembly.FullName);
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

        private static ICollection<IOTelExtension> GetExtensions(Assembly assembly)
        {
            var extensionType = typeof(IOTelExtension);

            return assembly
                .GetTypes()
                .Where(p => extensionType.IsAssignableFrom(p))
                .Select(p => (IOTelExtension)Activator.CreateInstance(p))
                .ToList();
        }
    }
}

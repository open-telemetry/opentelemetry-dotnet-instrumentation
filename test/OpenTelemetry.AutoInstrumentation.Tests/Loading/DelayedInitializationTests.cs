// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Loading.Initializers;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Tests.Loading;

public class DelayedInitializationTests
{
    [Theory]
    [InlineData("SqlClient")]
    [InlineData("HttpClient")]
#if NETFRAMEWORK
    [InlineData("AspNet")]
#endif
    public void ConfiguresTracingOnlyOnceAfterRegistration(string instrumentation)
    {
        PreloadAssembly(instrumentation);
        var (pluginManager, plugin) = CreatePluginManager();

        Action<LazyInstrumentationLoader> register = instrumentation switch
        {
            "SqlClient" => new SqlClientTracerInitializer(pluginManager).Register,
            "HttpClient" => new HttpClientInitializer(pluginManager, new TracerSettings()).Register,
#if NETFRAMEWORK
            "AspNet" => new AspNetInitializer(pluginManager, new TracerSettings()).Register,
#endif
            _ => throw new ArgumentException("Unsupported instrumentation.", nameof(instrumentation))
        };

        Assert.Equal(0, plugin.TracesConfigured);

        using var loader = new LazyInstrumentationLoader();
        register(loader);
        register(loader);

        Assert.Equal(1, plugin.TracesConfigured);
    }

    [Theory]
    [InlineData("SqlClient")]
    [InlineData("HttpClient")]
#if NETFRAMEWORK
    [InlineData("AspNet")]
#endif
    public void ConfiguresTracingWhenAssemblyAlreadyLoaded(string instrumentation)
    {
        PreloadAssembly(instrumentation);
        var (pluginManager, plugin) = CreatePluginManager();

        using var loader = new LazyInstrumentationLoader();
        Register(instrumentation, metrics: false, loader, pluginManager);

        Assert.Equal(1, plugin.TracesConfigured);
    }

    [Theory]
    [InlineData("SqlClient", false)]
    [InlineData("SqlClient", true)]
#if NETFRAMEWORK
    [InlineData("AspNet", false)]
    [InlineData("AspNet", true)]
#endif
    public void TracksAndDisposesOneHandleWhenAssemblyAlreadyLoaded(string instrumentation, bool metrics)
    {
        PreloadAssembly(instrumentation);
        var pluginManager = new PluginManager(new PluginsSettings());
        var handleManager = GetHandleManager(instrumentation);
        var property = handleManager.GetType().GetProperty(metrics ? "MetricHandles" : "TracingHandles")!;
        var initialCount = (int)property.GetValue(handleManager)!;

        using (var loader = new LazyInstrumentationLoader())
        {
            Register(instrumentation, metrics, loader, pluginManager);
            Assert.Equal(initialCount + 1, (int)property.GetValue(handleManager)!);
        }

        Assert.Equal(initialCount, (int)property.GetValue(handleManager)!);
    }

    private static void PreloadAssembly(string instrumentation)
    {
        switch (instrumentation)
        {
            case "SqlClient":
#if NETFRAMEWORK
                Assembly.Load("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
#else
                Assembly.Load("System.Data.SqlClient");
#endif
                break;
            case "HttpClient":
                _ = typeof(System.Net.Http.HttpClient).Assembly;
                break;
#if NETFRAMEWORK
            case "AspNet":
                Assembly.Load("System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                break;
#endif
            default:
                throw new ArgumentException("Unsupported instrumentation.", nameof(instrumentation));
        }
    }

    private static void Register(string instrumentation, bool metrics, LazyInstrumentationLoader loader, PluginManager pluginManager)
    {
        switch (instrumentation)
        {
            case "SqlClient" when metrics:
                DelayedInitialization.Metrics.AddSqlClient(loader, pluginManager);
                break;
            case "SqlClient":
                DelayedInitialization.Traces.AddSqlClient(loader, pluginManager);
                break;
            case "HttpClient":
                DelayedInitialization.Traces.AddHttpClient(loader, pluginManager, new TracerSettings());
                break;
#if NETFRAMEWORK
            case "AspNet" when metrics:
                DelayedInitialization.Metrics.AddAspNet(loader, pluginManager);
                break;
            case "AspNet":
                DelayedInitialization.Traces.AddAspNet(loader, pluginManager, new TracerSettings());
                break;
#endif
            default:
                throw new ArgumentException("Unsupported instrumentation.", nameof(instrumentation));
        }
    }

    private static object GetHandleManager(string instrumentation)
    {
        // The upstream singleton is internal, so inspect its handle counts through reflection.
        var type = Type.GetType($"OpenTelemetry.Instrumentation.{instrumentation}.{instrumentation}Instrumentation, OpenTelemetry.Instrumentation.{instrumentation}")!;
        var instance = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!;
        return type.GetField("HandleManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(instance)!;
    }

    private static (PluginManager Manager, OptionsPlugin Plugin) CreatePluginManager()
    {
        var settings = new PluginsSettings();
        settings.Plugins.Add(typeof(OptionsPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(settings);
        var plugin = Assert.IsType<OptionsPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        return (pluginManager, plugin);
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
#pragma warning disable CA1034 // Nested types should not be visible. Needed for plugin loading.
    public class OptionsPlugin : IPlugin, IConfigureTracesOptions<object>
#pragma warning restore CA1034 // Nested types should not be visible. Needed for plugin loading.
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
    {
        public int TracesConfigured { get; private set; }

        public void Initializing()
        {
        }

        public void Initialized()
        {
        }

        public void ConfigureTracesOptions(object options)
        {
            Assert.NotNull(options);
            TracesConfigured++;
        }
    }
}

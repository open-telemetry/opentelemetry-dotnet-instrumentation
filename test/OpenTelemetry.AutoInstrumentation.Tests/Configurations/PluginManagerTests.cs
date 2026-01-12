// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class PluginManagerTests
{
    [Fact]
    public void MissingAssembly()
    {
        var pluginAssemblyQualifiedName = "Missing.Assembly.PluginType, Missing.Assembly";
        var settings = GetSettings(pluginAssemblyQualifiedName);

        Assert.Throws<FileNotFoundException>(() => new PluginManager(settings));
    }

    [Fact]
    public void MissingPluginTypeFromAssembly()
    {
        var pluginAssemblyQualifiedName = "Missing.PluginType";
        var settings = GetSettings(pluginAssemblyQualifiedName);

        Assert.Throws<TypeLoadException>(() => new PluginManager(settings));
    }

    [Fact]
    public void MissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(PluginWithoutDefaultConstructor).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);

        Assert.Throws<MissingMethodException>(() => new PluginManager(settings));
    }

    [Fact]
    public void PluginTypeMissingMethodDoesNotThrow()
    {
        var pluginAssemblyQualifiedName = GetType().AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        Assert.Null(Record.Exception(() => Sdk.CreateTracerProviderBuilder().InvokePluginsBefore(pluginManager)));
        Assert.Null(Record.Exception(() => Sdk.CreateMeterProviderBuilder().InvokePluginsBefore(pluginManager)));
        Assert.Null(Record.Exception(() => Sdk.CreateTracerProviderBuilder().InvokePluginsAfter(pluginManager)));
        Assert.Null(Record.Exception(() => Sdk.CreateMeterProviderBuilder().InvokePluginsAfter(pluginManager)));
        Assert.Null(Record.Exception(() => ResourceBuilder.CreateEmpty().InvokePlugins(pluginManager)));
    }

    [Fact]
    public void PluginTypeMissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(MockPluginMissingDefaultConstructor).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);

        Assert.Throws<MissingMethodException>(() => new PluginManager(settings));
    }

    [Fact]
    public void InvokeProviderPluginSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var tracerProviderBuilderMock = Substitute.For<TracerProviderBuilder>();
        var meterProviderBuilderMock = Substitute.For<MeterProviderBuilder>();

        Assert.Null(Record.Exception(() => tracerProviderBuilderMock.InvokePluginsBefore(pluginManager)));
        Assert.Null(Record.Exception(() => meterProviderBuilderMock.InvokePluginsBefore(pluginManager)));
        Assert.Null(Record.Exception(() => tracerProviderBuilderMock.InvokePluginsAfter(pluginManager)));
        Assert.Null(Record.Exception(() => meterProviderBuilderMock.InvokePluginsAfter(pluginManager)));

        tracerProviderBuilderMock.Received(1).AddSource(Arg.Is<string>(x => x == "My.Custom.Before.Source"));
        tracerProviderBuilderMock.Received(1).AddSource(Arg.Is<string>(x => x == "My.Custom.After.Source"));
        meterProviderBuilderMock.Received(1).AddMeter(Arg.Is<string>(x => x == "My.Custom.Before.Meter"));
        meterProviderBuilderMock.Received(1).AddMeter(Arg.Is<string>(x => x == "My.Custom.After.Meter"));
    }

    [Fact]
    public void InvokeInitializationEvents()
    {
        var tracerProviderMock = Substitute.For<TracerProvider>();
        var meterProviderMock = Substitute.For<MeterProvider>();

        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        Assert.Null(Record.Exception(() => pluginManager.Initializing()));
        Assert.Null(Record.Exception(() => pluginManager.InitializedProvider(tracerProviderMock)));
        Assert.Null(Record.Exception(() => pluginManager.InitializedProvider(meterProviderMock)));

        var plugin = pluginManager.Plugins
            .Single(x => x.Type.IsAssignableFrom(typeof(MockPlugin)))
            .Instance as MockPlugin;

        Assert.NotNull(plugin);

        Assert.True(plugin.IsInitializingCalled);
        Assert.True(plugin.IsTracerProviderInitializedCalled);
        Assert.True(plugin.IsMeterProviderInitializedCalled);
    }

    [Fact]
    public void ConfigureLogsOptionsSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var logsAction = () =>
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = false;
                    pluginManager.ConfigureLogsOptions(options);

                    // Verify that plugin changes the state
                    Assert.True(options.IncludeFormattedMessage);
                });
            });
        };

        Assert.Null(Record.Exception(() => logsAction()));
    }

    [Fact]
    public void ConfigureResourceSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var resource = ResourceBuilder.CreateEmpty().InvokePlugins(pluginManager).Build();

        Assert.Equal("key", resource.Attributes.First().Key);
        Assert.Equal("value", resource.Attributes.First().Value);
    }

    private static PluginsSettings GetSettings(string assemblyQualifiedName)
    {
        var config = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection()
        {
            { ConfigurationKeys.ProviderPlugins, assemblyQualifiedName }
        }));

        var settings = new PluginsSettings();
        settings.LoadEnvVar(config);
        return settings;
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin purposes.
#pragma warning disable CA1034 // Nested types should not be visible. It is used only for test purposes.
    public class MockPlugin
#pragma warning restore CA1034 // Nested types should not be visible. It is used only for test purposes.
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin purposes.
    {
        public bool IsInitializingCalled { get; private set; }

        public bool IsTracerProviderInitializedCalled { get; private set; }

        public bool IsMeterProviderInitializedCalled { get; private set; }

        public void Initializing()
        {
            if (IsInitializingCalled)
            {
                throw new InvalidOperationException("Already called");
            }

            IsInitializingCalled = true;
        }

        public void TracerProviderInitialized(TracerProvider tracerProvider)
        {
            if (IsTracerProviderInitializedCalled)
            {
                throw new InvalidOperationException("Already called");
            }

            IsTracerProviderInitializedCalled = true;
        }

        public void MeterProviderInitialized(MeterProvider meterProvider)
        {
            if (IsMeterProviderInitializedCalled)
            {
                throw new InvalidOperationException("Already called");
            }

            IsMeterProviderInitializedCalled = true;
        }

#pragma warning disable CA1822 // Mark members as static. It is needed for plugin purposes.
        public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
        {
#if NET
            ArgumentNullException.ThrowIfNull(builder);
#else
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            builder.AddSource("My.Custom.Before.Source");

            return builder;
        }

        public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
        {
#if NET
            ArgumentNullException.ThrowIfNull(builder);
#else
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            builder.AddMeter("My.Custom.Before.Meter");

            return builder;
        }

        public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
        {
#if NET
            ArgumentNullException.ThrowIfNull(builder);
#else
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            builder.AddSource("My.Custom.After.Source");

            return builder;
        }

        public MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
        {
#if NET
            ArgumentNullException.ThrowIfNull(builder);
#else
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            builder.AddMeter("My.Custom.After.Meter");

            return builder;
        }

        public void ConfigureLogsOptions(OpenTelemetryLoggerOptions options)
        {
#if NET
            ArgumentNullException.ThrowIfNull(options);
#else
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
#endif

            // Dummy overwritten setting
            options.IncludeFormattedMessage = true;
        }

        public ResourceBuilder ConfigureResource(ResourceBuilder builder)
#pragma warning restore CA1822 // Mark members as static. It is needed for plugin purposes.
        {
            var attributes = new List<KeyValuePair<string, object>>
            {
                new("key", "value"),
            };

            builder.AddAttributes(attributes);
            return builder;
        }
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin purposes.
#pragma warning disable CA1034 // Nested types should not be visible. It is used only for test purposes.
    public class PluginWithoutDefaultConstructor
#pragma warning restore CA1034 // Nested types should not be visible. It is used only for test purposes.
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin purposes.
    {
        private readonly string _dummyParameter;

        public PluginWithoutDefaultConstructor(string dummyParameter)
        {
            _dummyParameter = dummyParameter;
        }

        public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
        {
#if NET
            ArgumentNullException.ThrowIfNull(builder);
#else
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
#endif

            builder.AddSource(_dummyParameter);

            return builder;
        }
    }

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin purposes.
#pragma warning disable CA1034 // Nested types should not be visible. It is used only for test purposes.
    public class MockPluginMissingDefaultConstructor : MockPlugin
#pragma warning restore CA1034 // Nested types should not be visible. It is used only for test purposes.
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin purposes.
    {
        public MockPluginMissingDefaultConstructor(string ignored)
        {
            throw new InvalidOperationException("this plugin is not expected to be successfully constructed");
        }
    }
}

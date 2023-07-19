// <copyright file="PluginManagerTests.cs" company="OpenTelemetry Authors">
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

using System.Collections.Specialized;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
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
        var createAction = () => new PluginManager(settings);

        using (new AssertionScope())
        {
            createAction.Should().Throw<FileNotFoundException>();
        }
    }

    [Fact]
    public void MissingPluginTypeFromAssembly()
    {
        var pluginAssemblyQualifiedName = "Missing.PluginType";
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var createAction = () => new PluginManager(settings);

        using (new AssertionScope())
        {
            createAction.Should().Throw<TypeLoadException>();
        }
    }

    [Fact]
    public void MissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(PluginWithoutDefaultConstructor).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var createAction = () => new PluginManager(settings);

        using (new AssertionScope())
        {
            createAction.Should().Throw<MissingMethodException>();
        }
    }

    [Fact]
    public void PluginTypeMissingMethodDoesNotThrow()
    {
        var pluginAssemblyQualifiedName = GetType().AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var tracerBeforeAction = () => Sdk.CreateTracerProviderBuilder().InvokePluginsBefore(pluginManager);
        var meterBeforeAction = () => Sdk.CreateMeterProviderBuilder().InvokePluginsBefore(pluginManager);
        var tracerAfterAction = () => Sdk.CreateTracerProviderBuilder().InvokePluginsAfter(pluginManager);
        var meterAfterAction = () => Sdk.CreateMeterProviderBuilder().InvokePluginsAfter(pluginManager);
        var resourceAction = () => ResourceBuilder.CreateEmpty().InvokePlugins(pluginManager);

        using (new AssertionScope())
        {
            tracerBeforeAction.Should().NotThrow();
            meterBeforeAction.Should().NotThrow();
            tracerAfterAction.Should().NotThrow();
            meterAfterAction.Should().NotThrow();
            resourceAction.Should().NotThrow();
        }
    }

    [Fact]
    public void PluginTypeMissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(MockPluginMissingDefaultConstructor).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var createAction = () => new PluginManager(settings);

        using (new AssertionScope())
        {
            createAction.Should().Throw<MissingMethodException>();
        }
    }

    [Fact]
    public void InvokeProviderPluginSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var tracerProviderBuilderMock = new Mock<TracerProviderBuilder>();
        var meterProviderBuilderMock = new Mock<MeterProviderBuilder>();

        var traceBeforeAction = () => tracerProviderBuilderMock.Object.InvokePluginsBefore(pluginManager);
        var meterBeforeAction = () => meterProviderBuilderMock.Object.InvokePluginsBefore(pluginManager);
        var traceAfterAction = () => tracerProviderBuilderMock.Object.InvokePluginsAfter(pluginManager);
        var meterAfterAction = () => meterProviderBuilderMock.Object.InvokePluginsAfter(pluginManager);

        using (new AssertionScope())
        {
            traceBeforeAction.Should().NotThrow();
            meterBeforeAction.Should().NotThrow();
            traceAfterAction.Should().NotThrow();
            meterAfterAction.Should().NotThrow();

            tracerProviderBuilderMock.Verify(x => x.AddSource(It.Is<string>(x => x == "My.Custom.Before.Source")), Times.Once);
            tracerProviderBuilderMock.Verify(x => x.AddSource(It.Is<string>(x => x == "My.Custom.After.Source")), Times.Once);
            meterProviderBuilderMock.Verify(x => x.AddMeter(It.Is<string>(x => x == "My.Custom.Before.Meter")), Times.Once);
            meterProviderBuilderMock.Verify(x => x.AddMeter(It.Is<string>(x => x == "My.Custom.After.Meter")), Times.Once);
        }
    }

    [Fact]
    public void InvokeInitializationEvents()
    {
        var tracerProviderMock = new Mock<TracerProvider>();
        var meterProviderMock = new Mock<MeterProvider>();

        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var initializingAction = () => pluginManager.Initializing();
        var tracerProviderInitializedAction = () => pluginManager.InitializedProvider(tracerProviderMock.Object);
        var meterProviderInitializedAction = () => pluginManager.InitializedProvider(meterProviderMock.Object);

        using (new AssertionScope())
        {
            initializingAction.Should().NotThrow();
            tracerProviderInitializedAction.Should().NotThrow();
            meterProviderInitializedAction.Should().NotThrow();

            var plugin = pluginManager.Plugins
                .Single(x => x.Type.IsAssignableFrom(typeof(MockPlugin)))
                .Instance
                .As<MockPlugin>();

            plugin.IsInitializingCalled.Should().BeTrue();
            plugin.IsTracerProviderInitializedCalled.Should().BeTrue();
            plugin.IsMeterProviderInitializedCalled.Should().BeTrue();
        }
    }

    [Fact]
    public void ConfigureLogsOptionsSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = false;
                pluginManager.ConfigureLogsOptions(options);

                // Verify that plugin changes the state
                options.IncludeFormattedMessage.Should().BeTrue();
            });
        });

        using (new AssertionScope())
        {
            logsAction.Should().NotThrow();
        }
    }

    [Fact]
    public void ConfigureResourceSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName!;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var resource = ResourceBuilder.CreateEmpty().InvokePlugins(pluginManager).Build();

        using (new AssertionScope())
        {
            resource.Attributes.First().Key.Should().Be("key");
            resource.Attributes.First().Value.Should().Be("value");
        }
    }

    private static GeneralSettings GetSettings(string assemblyQualifiedName)
    {
        var config = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection()
        {
            { ConfigurationKeys.ProviderPlugins, assemblyQualifiedName }
        }));

        var settings = new GeneralSettings();
        settings.Load(config);
        return settings;
    }

    public class MockPlugin
    {
        public bool IsInitializingCalled { get; private set; } = false;

        public bool IsTracerProviderInitializedCalled { get; private set; } = false;

        public bool IsMeterProviderInitializedCalled { get; private set; } = false;

        public void Initializing()
        {
            if (IsInitializingCalled)
            {
                throw new InvalidOperationException("Allready called");
            }

            IsInitializingCalled = true;
        }

        public void TracerProviderInitialized(TracerProvider tracerProvider)
        {
            if (IsTracerProviderInitializedCalled)
            {
                throw new InvalidOperationException("Allready called");
            }

            IsTracerProviderInitializedCalled = true;
        }

        public void MeterProviderInitialized(MeterProvider meterProvider)
        {
            if (IsMeterProviderInitializedCalled)
            {
                throw new InvalidOperationException("Allready called");
            }

            IsMeterProviderInitializedCalled = true;
        }

        public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
        {
            builder.AddSource("My.Custom.Before.Source");

            return builder;
        }

        public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
        {
            builder.AddMeter("My.Custom.Before.Meter");

            return builder;
        }

        public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
        {
            builder.AddSource("My.Custom.After.Source");

            return builder;
        }

        public MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
        {
            builder.AddMeter("My.Custom.After.Meter");

            return builder;
        }

        public void ConfigureLogsOptions(OpenTelemetryLoggerOptions options)
        {
            // Dummy overwritten setting
            options.IncludeFormattedMessage = true;
        }

        public ResourceBuilder ConfigureResource(ResourceBuilder builder)
        {
            var attributes = new List<KeyValuePair<string, object>>
            {
                new("key", "value"),
            };

            builder.AddAttributes(attributes);
            return builder;
        }
    }

    public class PluginWithoutDefaultConstructor
    {
        private readonly string _dummyParameter;

        public PluginWithoutDefaultConstructor(string dummyParameter)
        {
            _dummyParameter = dummyParameter;
        }

        public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
        {
            builder.AddSource(_dummyParameter);

            return builder;
        }
    }

    public class MockPluginMissingDefaultConstructor : MockPlugin
    {
        public MockPluginMissingDefaultConstructor(string ignored)
        {
            throw new InvalidOperationException("this plugin is not expected to be successfully constructed");
        }
    }
}

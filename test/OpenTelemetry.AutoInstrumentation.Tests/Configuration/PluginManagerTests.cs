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

using System;
using System.Collections.Specialized;
using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configuration;

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
    public void PluginTypeMissingMethodDoesNotThrow()
    {
        var pluginAssemblyQualifiedName = GetType().AssemblyQualifiedName;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(pluginManager);
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(pluginManager);

        using (new AssertionScope())
        {
            tracerAction.Should().NotThrow();
            meterAction.Should().NotThrow();
        }
    }

    [Fact]
    public void PluginTypeMissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(MockPluginMissingDefaultConstructor).AssemblyQualifiedName;
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
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName;
        var settings = GetSettings(pluginAssemblyQualifiedName);
        var pluginManager = new PluginManager(settings);

        var traceProviderBuilderMock = new Mock<TracerProviderBuilder>();
        traceProviderBuilderMock.Setup(x => x.AddSource(It.Is<string>(x => x == "My.Custom.Source"))).Verifiable();

        var meterProviderBuilderMock = new Mock<MeterProviderBuilder>();
        meterProviderBuilderMock.Setup(x => x.AddMeter(It.Is<string>(x => x == "My.Custom.Meter"))).Verifiable();

        var tracerAction = () => traceProviderBuilderMock.Object.InvokePlugins(pluginManager);
        var meterAction = () => meterProviderBuilderMock.Object.InvokePlugins(pluginManager);

        using (new AssertionScope())
        {
            tracerAction.Should().NotThrow();
            meterAction.Should().NotThrow();

            traceProviderBuilderMock.Verify();
            meterProviderBuilderMock.Verify();
        }
    }

    [Fact]
    public void ConfigureLogsOptionsSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName;
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

    private static GeneralSettings GetSettings(string assemblyQualifiedName)
    {
        var config = new NameValueConfigurationSource(new NameValueCollection()
        {
            { ConfigurationKeys.ProviderPlugins, assemblyQualifiedName }
        });

        return new GeneralSettings(config);
    }

    public class MockPlugin
    {
        public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
        {
            builder.AddSource("My.Custom.Source");

            return builder;
        }

        public MeterProviderBuilder ConfigureMeterProvider(MeterProviderBuilder builder)
        {
            builder.AddMeter("My.Custom.Meter");

            return builder;
        }

        public void ConfigureLoggingOptions(OpenTelemetryLoggerOptions options)
        {
            // Dummy overwritten setting
            options.IncludeFormattedMessage = true;
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

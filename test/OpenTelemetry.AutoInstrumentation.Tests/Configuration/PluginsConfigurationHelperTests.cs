// <copyright file="PluginsConfigurationHelperTests.cs" company="OpenTelemetry Authors">
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
using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configuration;

public class PluginsConfigurationHelperTests
{
    [Fact]
    public void MissingAssembly()
    {
        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { "Missing.Assembly.PluginType, Missing.Assembly" });
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { "Missing.Assembly.PluginType, Missing.Assembly" });
        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.InvokePlugins(new[] { "Missing.Assembly.PluginType, Missing.Assembly" });
            });
        });

        using (new AssertionScope())
        {
            tracerAction.Should().Throw<FileNotFoundException>();
            meterAction.Should().Throw<FileNotFoundException>();
            logsAction.Should().Throw<FileNotFoundException>();
        }
    }

    [Fact]
    public void MissingPluginTypeFromAssembly()
    {
        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { "Missing.PluginType" });
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { "Missing.PluginType" });
        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.InvokePlugins(new[] { "Missing.PluginType" });
            });
        });

        using (new AssertionScope())
        {
            tracerAction.Should().Throw<TypeLoadException>();
            meterAction.Should().Throw<TypeLoadException>();
            logsAction.Should().Throw<TypeLoadException>();
        }
    }

    [Fact]
    public void PluginTypeMissingExpectedMethod()
    {
        var pluginAssemblyQualifiedName = GetType().AssemblyQualifiedName;
        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.InvokePlugins(new[] { pluginAssemblyQualifiedName });
            });
        });

        using (new AssertionScope())
        {
            tracerAction.Should().Throw<MissingMethodException>();
            meterAction.Should().Throw<MissingMethodException>();
            logsAction.Should().Throw<MissingMethodException>();
        }
    }

    [Fact]
    public void PluginTypeMissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(MockPluginMissingDefaultConstructor).AssemblyQualifiedName;
        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.InvokePlugins(new[] { pluginAssemblyQualifiedName });
            });
        });

        using (new AssertionScope())
        {
            tracerAction.Should().Throw<MissingMethodException>();
            meterAction.Should().Throw<MissingMethodException>();
            logsAction.Should().Throw<MissingMethodException>();
        }
    }

    [Fact]
    public void InvokePluginSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName;
        var tracerAction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var meterAction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        var logsAction = () => LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.InvokePlugins(new[] { pluginAssemblyQualifiedName });
            });
        });

        using (new AssertionScope())
        {
            tracerAction.Should().NotThrow();
            meterAction.Should().NotThrow();
            logsAction.Should().NotThrow();
        }
    }

    public class MockPlugin
    {
        public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
        {
            return builder;
        }

        public MeterProviderBuilder ConfigureMeterProvider(MeterProviderBuilder builder)
        {
            return builder;
        }

        public OpenTelemetryLoggerOptions ConfigureLoggerOptions(OpenTelemetryLoggerOptions options)
        {
            return options;
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

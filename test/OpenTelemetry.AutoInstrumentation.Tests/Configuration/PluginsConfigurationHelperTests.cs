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
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configuration;

public class PluginsConfigurationHelperTests
{
    [Fact]
    public void MissingAssembly()
    {
        var traceraction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { "Missing.Assembly.PluginType, Missing.Assembly" });
        traceraction.Should().Throw<FileNotFoundException>();

        var meteraction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { "Missing.Assembly.PluginType, Missing.Assembly" });
        meteraction.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void MissingPluginTypeFromAssembly()
    {
        var traceraction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { "Missing.PluginType" });
        traceraction.Should().Throw<TypeLoadException>();

        var meteraction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { "Missing.PluginType" });
        meteraction.Should().Throw<TypeLoadException>();
    }

    [Fact]
    public void PluginTypeMissingExpectedMethod()
    {
        var pluginAssemblyQualifiedName = GetType().AssemblyQualifiedName;
        var traceraction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        traceraction.Should().Throw<MissingMethodException>();

        var meteraction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        meteraction.Should().Throw<MissingMethodException>();
    }

    [Fact]
    public void PluginTypeMissingDefaultConstructor()
    {
        var pluginAssemblyQualifiedName = typeof(MockPluginMissingDefaultConstructor).AssemblyQualifiedName;
        var traceraction = () => Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        traceraction.Should().Throw<MissingMethodException>();

        var meteraction = () => Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        meteraction.Should().Throw<MissingMethodException>();
    }

    [Fact]
    public void InvokePluginSuccess()
    {
        var pluginAssemblyQualifiedName = typeof(MockPlugin).AssemblyQualifiedName;
        Sdk.CreateTracerProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
        Sdk.CreateMeterProviderBuilder().InvokePlugins(new[] { pluginAssemblyQualifiedName });
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
    }

    public class MockPluginMissingDefaultConstructor : MockPlugin
    {
        public MockPluginMissingDefaultConstructor(string ignored)
        {
            throw new InvalidOperationException("this plugin is not expected to be successfully constructed");
        }
    }
}

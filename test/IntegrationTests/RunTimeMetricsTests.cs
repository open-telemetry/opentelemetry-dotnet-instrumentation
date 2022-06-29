// <copyright file="RunTimeMetricsTests.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Opentelemetry.Proto.Common.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.RunTimeMetrics;

public class RunTimeMetricsTests : TestHelper
{
    private const string ServiceName = "TestApplication.StartupHook";

    public RunTimeMetricsTests(ITestOutputHelper output)
        : base("StartupHook", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS", "NetRuntime");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        var collectorPort = TcpPortProvider.GetOpenPort();
        using var collector = new MockCollector(Output, collectorPort);

        const int expectedMetricRequests = 1;

        var testSettings = new TestSettings
        {
            MetricsSettings = new MetricsSettings { Port = collectorPort },
            EnableStartupHook = true,
        };

        using var processResult = RunTestApplicationAndWaitForExit(testSettings);
        Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");
        var metricRequests = collector.WaitForMetrics(expectedMetricRequests, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            metricRequests.Count.Should().BeGreaterThanOrEqualTo(expectedMetricRequests);
            var resourceMetrics = metricRequests.FirstOrDefault().ResourceMetrics.Single();

            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = ServiceName } };
            resourceMetrics.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);
            var runtimeMetricsScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("OpenTelemetry.Instrumentation.Runtime", StringComparison.OrdinalIgnoreCase));

            // Assert on few runtime metrics which are common to .NET and .NET Core.
            var processCountMetric = runtimeMetricsScope.Metrics.FirstOrDefault(m => m.Name.Equals("process.cpu.count", StringComparison.OrdinalIgnoreCase));
            processCountMetric.Should().NotBeNull();
            processCountMetric.DataCase.Should().Be(Opentelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Gauge);
            processCountMetric.Gauge.DataPoints.Count.Should().BeGreaterThanOrEqualTo(1);

            var dotnetAssemblyCountMetric = runtimeMetricsScope.Metrics.FirstOrDefault(m => m.Name.Equals("process.runtime.dotnet.assembly.count", StringComparison.OrdinalIgnoreCase));
            dotnetAssemblyCountMetric.Should().NotBeNull();
            dotnetAssemblyCountMetric.DataCase.Should().Be(Opentelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Sum);
            dotnetAssemblyCountMetric.Sum.DataPoints.Count.Should().BeGreaterThanOrEqualTo(1);

            var dotnetGCHeapMetric = runtimeMetricsScope.Metrics.FirstOrDefault(m => m.Name.Equals("process.runtime.dotnet.gc.heap", StringComparison.OrdinalIgnoreCase));
            dotnetGCHeapMetric.Should().NotBeNull();
            dotnetGCHeapMetric.DataCase.Should().Be(Opentelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Gauge);
            dotnetGCHeapMetric.Gauge.DataPoints.Count.Should().BeGreaterThanOrEqualTo(1);
        }
    }
}

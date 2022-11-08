// <copyright file="SmokeTests.cs" company="OpenTelemetry Authors">
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

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

#if NETFRAMEWORK
using IntegrationTests.Helpers.Compatibility;
#else
using System;
using System.Collections.Generic;
#endif

namespace IntegrationTests;

public class SmokeTests : TestHelper
{
    private const string ServiceName = "TestApplication.Smoke";

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        VerifyTestApplicationInstrumented();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void WhenStartupHookIsNotEnabled()
    {
        SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", null);
#if NETFRAMEWORK
        VerifyTestApplicationInstrumented();
#else
        // on .NET Core it is required to set DOTNET_STARTUP_HOOKS
        VerifyTestApplicationNotInstrumented();
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void WhenClrProfilerIsNotEnabled()
    {
        SetEnvironmentVariable("COR_ENABLE_PROFILING", "0");
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0");
#if NETFRAMEWORK
        // on .NET Framework it is required to set the CLR .NET Profiler
        VerifyTestApplicationNotInstrumented();
#else
        VerifyTestApplicationInstrumented();
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsNotExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");

        VerifyTestApplicationInstrumented();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");

        VerifyTestApplicationNotInstrumented();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsNotIncluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES", "dotnet,dotnet.exe");

#if NETFRAMEWORK
        VerifyTestApplicationNotInstrumented();
#else
        // FIXME: OTEL_DOTNET_AUTO_INCLUDE_PROCESSES does not work on .NET Core.
        // https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/895
        VerifyTestApplicationInstrumented();
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsIncluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES", $"{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");

        VerifyTestApplicationInstrumented();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", metric => metric.Name == "MyFruitCounter");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TracesResource()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MetricsResource()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }

#if !NETFRAMEWORK // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsResource()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.ResourceExpector.AssertExpectations();
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OtlpTracesExporter()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", span => span.Name == "SayHello");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ZipkinExporter()
    {
        using var collector = new MockZipkinCollector(Output);
        collector.Expect(span => span.Name == "SayHello" && span.Tags.GetValueOrDefault("otel.library.name") == "MyCompany.MyProduct.MyLibrary");

        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "zipkin");
        SetEnvironmentVariable("OTEL_EXPORTER_ZIPKIN_ENDPOINT", $"http://localhost:{collector.Port}/api/v2/spans");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

#if NETFRAMEWORK // The test is flaky on Linux and macOS, becasue of https://github.com/dotnet/runtime/issues/28658#issuecomment-462062760
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void PrometheusExporter()
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "prometheus");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const string defaultPrometheusMetricsEndpoint = "http://localhost:9464/metrics";

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            var assert = async () =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = 5.Seconds()
                };
                var response = await httpClient.GetAsync(defaultPrometheusMetricsEndpoint);
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine("Raw metrics from Prometheus:");
                Output.WriteLine(content);
                content.Should().Contain("TYPE ", "should export any metric");
            };
            assert.Should().NotThrowAfterAsync(
                waitTime: 1.Minutes(),
                pollInterval: 1.Seconds());
        }
        finally
        {
            if (!helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();
            }

            Output.WriteLine("ProcessId: " + helper.Process.Id);
            Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
            Output.WriteResult(helper);
        }
    }
#endif

#if !NETFRAMEWORK // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Example log message\" }");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsNoneInstrumentations()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLED_INSTRUMENTATIONS", "none");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertEmpty(5.Seconds());
    }
#endif

    private void VerifyTestApplicationInstrumented()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.HttpWebRequest");
#else
        collector.Expect("OpenTelemetry.Instrumentation.Http");
#endif

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    private void VerifyTestApplicationNotInstrumented()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertEmpty(5.Seconds());
    }
}

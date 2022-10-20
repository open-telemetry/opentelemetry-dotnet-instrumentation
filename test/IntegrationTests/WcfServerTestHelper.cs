// <copyright file="WcfServerTestHelper.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.IO;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

internal class WcfServerTestHelper : TestHelper
{
    private const string ServiceName = "TestApplication.Wcf.Server.NetFramework";

    public WcfServerTestHelper(ITestOutputHelper output)
        : base("Wcf.Server.NetFramework", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    public ProcessHelper RunWcfServer(int traceAgentPort)
    {
        var projectDirectory = EnvironmentHelper.GetTestApplicationProjectDirectory();
        var exeFileName = $"{EnvironmentHelper.FullTestApplicationName}.exe";
        var testApplicationPath = Path.Combine(projectDirectory, "bin", EnvironmentTools.GetPlatform().ToLowerInvariant(), EnvironmentTools.GetBuildConfiguration(), "net462", exeFileName);

        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"Unable to find executing assembly at {testApplicationPath}");
        }

        var testSettings = new TestSettings
        {
            OtlpTracesSettings = new OtlpTracesSettings { Port = traceAgentPort }
        };

        var startInfo = new ProcessStartInfo(testApplicationPath, null);

        SetEnvironmentVariables(testSettings, startInfo.EnvironmentVariables, testApplicationPath);

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = false;

        var process = Process.Start(startInfo);
        var processHelper = new ProcessHelper(process);
        return processHelper;
    }

    private void SetEnvironmentVariables(
        TestSettings testSettings,
        StringDictionary environmentVariables,
        string processToProfile)
    {
        string profilerPath = EnvironmentHelper.GetProfilerPath();

        if (testSettings.EnableClrProfiler)
        {
            environmentVariables["COR_ENABLE_PROFILING"] = "1";
            environmentVariables["COR_PROFILER"] = EnvironmentTools.ProfilerClsId;
            environmentVariables["COR_PROFILER_PATH"] = profilerPath;
        }

        if (EnvironmentHelper.DebugModeEnabled)
        {
            environmentVariables["OTEL_DOTNET_AUTO_DEBUG"] = "1";
            environmentVariables["OTEL_DOTNET_AUTO_LOG_DIRECTORY"] = Path.Combine(EnvironmentTools.GetSolutionDirectory(), "build_data", "profiler-logs");
        }

        if (!string.IsNullOrEmpty(processToProfile))
        {
            environmentVariables["OTEL_DOTNET_AUTO_INCLUDE_PROCESSES"] = Path.GetFileName(processToProfile);
        }

        string integrations = EnvironmentHelper.GetIntegrationsPath();
        environmentVariables["OTEL_DOTNET_AUTO_HOME"] = EnvironmentHelper.GetNukeBuildOutput();
        environmentVariables["OTEL_DOTNET_AUTO_INTEGRATIONS_FILE"] = integrations;

        if (testSettings.TracesSettings != null)
        {
            environmentVariables["OTEL_TRACES_EXPORTER"] = testSettings.TracesSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_ZIPKIN_ENDPOINT"] = $"http://localhost:{testSettings.TracesSettings.Port}/api/v2/spans";
        }

        if (testSettings.OtlpTracesSettings != null)
        {
            environmentVariables["OTEL_TRACES_EXPORTER"] = testSettings.OtlpTracesSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.OtlpTracesSettings.Port}";
        }

        if (testSettings.MetricsSettings != null)
        {
            environmentVariables["OTEL_METRICS_EXPORTER"] = testSettings.MetricsSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.MetricsSettings.Port}";
        }

        if (testSettings.LogSettings != null)
        {
            environmentVariables["OTEL_LOGS_EXPORTER"] = testSettings.LogSettings.Exporter;
            environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://localhost:{testSettings.LogSettings.Port}";
        }

        environmentVariables["OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES"] = "TestApplication.*";

        foreach (var key in EnvironmentHelper.CustomEnvironmentVariables.Keys)
        {
            environmentVariables[key] = EnvironmentHelper.CustomEnvironmentVariables[key];
        }
    }
}

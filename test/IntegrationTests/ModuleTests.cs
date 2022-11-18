// <copyright file="ModuleTests.cs" company="OpenTelemetry Authors">
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

using System.IO;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Newtonsoft.Json;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configuration;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[UsesVerify]
public class ModuleTests : TestHelper
{
    public ModuleTests(ITestOutputHelper output)
        : base("Modules", output)
    {
    }

    [Fact]
    public async Task Default()
    {
        EnableDefaultExporters();
        EnableBytecodeInstrumentation();

        string verifyTestName =
#if NETFRAMEWORK
        $"{nameof(ModuleTests)}.{nameof(Default)}.NetFx";
#else
        $"{nameof(ModuleTests)}.{nameof(Default)}.NetCore";
#endif

        await RunTests(verifyTestName);
    }

    [Fact]
    public async Task DefaultNoExporters()
    {
        // Exporters using RPC can cause dependency loads, which can cause next instrumentation loads.
        // This test ensures correct instrumentation loading.

        SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, Constants.ConfigurationValues.None);

        string verifyTestName =
#if NETFRAMEWORK
        $"{nameof(ModuleTests)}.{nameof(DefaultNoExporters)}.NetFx";
#else
$"{nameof(ModuleTests)}.{nameof(DefaultNoExporters)}.NetCore";
#endif

        await RunTests(verifyTestName);
    }

    [Fact]
    public async Task Minimal()
    {
        SetEnvironmentVariable(ConfigurationKeys.Traces.Instrumentations, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Metrics.Instrumentations, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Traces.ConsoleExporterEnabled, bool.FalseString);
        SetEnvironmentVariable(ConfigurationKeys.Metrics.ConsoleExporterEnabled, bool.FalseString);
        SetEnvironmentVariable(ConfigurationKeys.Traces.OpenTracingEnabled, bool.FalseString);

        string verifyTestName =
#if NETFRAMEWORK
        $"{nameof(ModuleTests)}.{nameof(Minimal)}.NetFx";
#else
        $"{nameof(ModuleTests)}.{nameof(Minimal)}.NetCore";
#endif

        await RunTests(verifyTestName);
    }

    private async Task RunTests(string verifyName)
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            RunTestApplication(new()
            {
                Arguments = $"--temp-path {tempPath}"
            });

            if (!File.Exists(tempPath))
            {
                Assert.Fail("Could not find modules report file.");
            }

            var json = File.ReadAllText(tempPath);
            var modules = JsonConvert.DeserializeObject<string[]>(json);

            await Verifier.Verify(modules)
                .UseFileName(verifyName)
                .DisableDiff();
        }
        finally
        {
            // Cleanup
            File.Delete(tempPath);
        }
    }
}

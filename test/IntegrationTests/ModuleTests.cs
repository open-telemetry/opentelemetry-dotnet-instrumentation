// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Newtonsoft.Json;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

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

        await RunTests(verifyTestName).ConfigureAwait(true);
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

        await RunTests(verifyTestName).ConfigureAwait(true);
    }

    [Fact]
    public async Task Minimal()
    {
        SetEnvironmentVariable(ConfigurationKeys.InstrumentationEnabled, "false");
        SetEnvironmentVariable(ConfigurationKeys.ResourceDetectorEnabled, "false");
        SetEnvironmentVariable(ConfigurationKeys.Traces.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Metrics.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Logs.Exporter, Constants.ConfigurationValues.None);
        SetEnvironmentVariable(ConfigurationKeys.Traces.OpenTracingEnabled, bool.FalseString);

        string verifyTestName =
#if NETFRAMEWORK
        $"{nameof(ModuleTests)}.{nameof(Minimal)}.NetFx";
#else
        $"{nameof(ModuleTests)}.{nameof(Minimal)}.NetCore";
#endif

        await RunTests(verifyTestName).ConfigureAwait(true);
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

#if NET
            var json = await File.ReadAllTextAsync(tempPath).ConfigureAwait(false);
#else
            var json = File.ReadAllText(tempPath);
#endif
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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Newtonsoft.Json;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit.Abstractions;

namespace IntegrationTests;

[UsesVerify]
public class ModuleTests : TestHelper
{
    public ModuleTests(ITestOutputHelper output)
        : base("Modules", output)
    {
    }

    public static TheoryData<int> GetData()
    {
        var theoryData = new TheoryData<int>();

        for (int i = 0; i < 100; i++)
        {
            theoryData.Add(i);
        }

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public async Task Default(int i)
    {
        EnableDefaultExporters();
        EnableBytecodeInstrumentation();

        _ = i;

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

        await RunTests(verifyTestName);
    }

    private Task RunTests(string verifyName)
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            RunTestApplication(new()
            {
                Arguments = $"--temp-path {tempPath}"
            });

            return Task.CompletedTask;
        }
        finally
        {
            // Cleanup
            File.Delete(tempPath);
        }
    }
}

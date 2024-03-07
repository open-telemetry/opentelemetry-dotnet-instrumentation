// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

    public ProcessHelper RunWcfServer(MockSpansCollector collector)
    {
        var baseBinDirectory = EnvironmentHelper.GetTestApplicationBaseBinDirectory();
        var exeFileName = $"{EnvironmentHelper.FullTestApplicationName}.exe";
        var testApplicationPath = Path.Combine(baseBinDirectory, EnvironmentTools.GetPlatformDir(), EnvironmentTools.GetBuildConfiguration(), "net462", exeFileName);

        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"Unable to find executing assembly at {testApplicationPath}");
        }

        SetExporter(collector);
        var process = InstrumentedProcessHelper.Start(testApplicationPath, null, EnvironmentHelper);
        return new ProcessHelper(process);
    }
}

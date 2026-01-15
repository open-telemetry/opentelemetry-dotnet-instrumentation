// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if _WINDOWS

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

#pragma warning disable CA1812 // Mark members as static. There is some issue in dotnet format.
// TODO remove pragma when dotnet format issue is fixed
internal sealed class WcfServerTestHelper : WcfServerTestHelperBase
#pragma warning restore CA1812 // Mark members as static. There is some issue in dotnet format.
{
    public WcfServerTestHelper(ITestOutputHelper output)
        : base("Wcf.Server.NetFramework", output, "TestApplication.Wcf.Server.NetFramework")
    {
    }

    internal override string ServerInstrumentationScopeName { get => "OpenTelemetry.Instrumentation.Wcf"; }

    internal override (ProcessHelper ProcessHelper, int TcpPort, int HttpPort) RunWcfServer(MockSpansCollector collector)
    {
        var baseBinDirectory = EnvironmentHelper.GetTestApplicationBaseBinDirectory();
        var exeFileName = $"{EnvironmentHelper.FullTestApplicationName}.exe";
        var testApplicationPath = Path.Combine(baseBinDirectory, EnvironmentTools.GetPlatformDir(), EnvironmentTools.GetBuildConfiguration(), "net462", exeFileName);

        if (!File.Exists(testApplicationPath))
        {
            throw new InvalidOperationException($"Unable to find executing assembly at {testApplicationPath}");
        }

        var httpPort = TcpPortProvider.GetOpenPort();
        var tcpPort = TcpPortProvider.GetOpenPort();

        SetExporter(collector);
#pragma warning disable CA2000 // Dispose objects before losing scope. Process is disposed by ProcessHelper.
        var process = InstrumentedProcessHelper.Start(testApplicationPath, $"--tcpPort {tcpPort} --httpPort {httpPort}", EnvironmentHelper);
#pragma warning restore CA2000 // Dispose objects before losing scope. Process is disposed by ProcessHelper.
        return (new ProcessHelper(process), tcpPort, httpPort);
    }
}
#endif

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

#pragma warning disable CA1812 // Mark members as static. There is some issue in dotnet format.
// TODO remove pragma when dotnet format issue is fixed
internal sealed class WcfCoreServerTestHelper : WcfServerTestHelperBase
#pragma warning restore CA1812 // Mark members as static. There is some issue in dotnet format.
{
    private readonly string _packageVersion;

    public WcfCoreServerTestHelper(ITestOutputHelper output, string packageVersion)
        : base("Wcf.Core", output, "TestApplication.Wcf.Core")
    {
        _packageVersion = packageVersion;
    }

    internal override string ServerInstrumentationScopeName { get => "CoreWCF.Primitives"; }

    internal override ProcessHelper RunWcfServer(MockSpansCollector collector)
    {
        var testApplicationPath = EnvironmentHelper.GetTestApplicationPath(_packageVersion, startupMode: TestAppStartupMode.Exe);

        if (!File.Exists(testApplicationPath))
        {
            throw new InvalidOperationException($"Unable to find executing assembly at {testApplicationPath}");
        }

        SetExporter(collector);
        var process = InstrumentedProcessHelper.Start(testApplicationPath, null, EnvironmentHelper);
        return new ProcessHelper(process);
    }
}
#endif

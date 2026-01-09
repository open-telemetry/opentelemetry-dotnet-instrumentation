// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

internal sealed class WcfCoreServerTestHelper : WcfServerTestHelperBase
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
            throw new Exception($"Unable to find executing assembly at {testApplicationPath}");
        }

        SetExporter(collector);
        var process = InstrumentedProcessHelper.Start(testApplicationPath, null, EnvironmentHelper);
        return new ProcessHelper(process);
    }
}

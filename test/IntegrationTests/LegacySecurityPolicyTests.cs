// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class LegacySecurityPolicyTests : TestHelper
{
    public LegacySecurityPolicyTests(ITestOutputHelper output)
        : base("LegacySecurityPolicy.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SkipsLoaderInLegacySecurityPolicyAppDomain()
    {
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "none");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "none");

        var (standardOutput, _, _) = RunTestApplication();

        Assert.Contains("LegacyDomainIsHomogenous=False", standardOutput, StringComparison.Ordinal);
        Assert.Contains("LegacyDomainIsFullyTrusted=True", standardOutput, StringComparison.Ordinal);
        Assert.Contains("LegacyDomainLoaderLoaded=False", standardOutput, StringComparison.Ordinal);
    }
}
#endif

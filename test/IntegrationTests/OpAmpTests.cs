// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpAmp.Proto.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class OpAmpTests : TestHelper
{
    public OpAmpTests(ITestOutputHelper output)
#if NET
        : base("Http", output)
#else
        : base("Http.NetFramework", output)
#endif
    {
    }

    [Fact]
    public void OpAmpClient_CanConnect()
    {
        using var server = new MockOpAmpServer(Output);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");
        SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "opamp.test=true");

        AgentDescription? agentDescriptionFrame = null;

        server.Expect(
            f =>
            {
                agentDescriptionFrame = f.AgentDescription;
                return f.AgentDescription != null;
            },
            "Has AgentDescription frame");

        server.Expect(f => f.AgentDisconnect != null, "Has AgentDisconnect frame");

        RunTestApplication();

        server.AssertExpectations();

        Assert.NotNull(agentDescriptionFrame);
        Assert.Contains(agentDescriptionFrame.NonIdentifyingAttributes, a => a.Key == "opamp.test");
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class ServerSentCapabilitiesStateTests
{
    [Fact]
    public void NoneDoesNotReplaceCurrentCapabilities()
    {
        var state = new ServerSentCapabilitiesState();
        var capabilities = ServerSentCapabilities.AcceptsEffectiveConfig |
                           ServerSentCapabilities.OffersRemoteConfig;

        Assert.Equal(capabilities, state.UpdateAndGetNewlyEnabled(capabilities));
        Assert.Equal(ServerSentCapabilities.None, state.UpdateAndGetNewlyEnabled(ServerSentCapabilities.None));
        Assert.Equal(capabilities, state.GetSnapshot());
    }

    [Fact]
    public void ReplacementReturnsOnlyNewlyEnabledCapabilities()
    {
        var state = new ServerSentCapabilitiesState();
        var initialCapabilities = ServerSentCapabilities.AcceptsEffectiveConfig |
                                  ServerSentCapabilities.OffersRemoteConfig;

        Assert.Equal(initialCapabilities, state.UpdateAndGetNewlyEnabled(initialCapabilities));
        Assert.Equal(ServerSentCapabilities.None, state.UpdateAndGetNewlyEnabled(initialCapabilities));

        Assert.Equal(
            ServerSentCapabilities.AcceptsStatus,
            state.UpdateAndGetNewlyEnabled(ServerSentCapabilities.AcceptsStatus));
        Assert.Equal(ServerSentCapabilities.AcceptsStatus, state.GetSnapshot());

        var reenabledCapabilities = ServerSentCapabilities.AcceptsStatus |
                                    ServerSentCapabilities.AcceptsEffectiveConfig;
        Assert.Equal(
            ServerSentCapabilities.AcceptsEffectiveConfig,
            state.UpdateAndGetNewlyEnabled(reenabledCapabilities));
        Assert.Equal(reenabledCapabilities, state.GetSnapshot());
    }
}

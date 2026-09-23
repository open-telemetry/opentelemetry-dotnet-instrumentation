// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp.Listeners;

internal sealed class CapabilitiesListener :
    IOpAmpListener<ServerCapabilitiesMessage>,
    IOpAmpListener<CustomCapabilitiesMessage>
{
    private readonly OpAmpManager _manager;

    public CapabilitiesListener(OpAmpManager manager)
    {
        _manager = manager;
    }

    public void HandleMessage(ServerCapabilitiesMessage message)
    {
        _manager.HandleServerCapabilities(message.Capabilities);
    }

    public void HandleMessage(CustomCapabilitiesMessage message)
    {
        _manager.HandleServerCustomCapabilities(message.Capabilities);
    }
}

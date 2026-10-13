// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

// Keeps plugin access limited to IOpAmpClient while OpAmpManager retains lifecycle ownership.
internal sealed class PluginOpAmpClient : IOpAmpClient
{
    private readonly OpAmpManager _manager;

    public PluginOpAmpClient(OpAmpManager manager)
    {
        _manager = manager;
    }

    public void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _manager.Subscribe(listener);
    }

    public void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _manager.Unsubscribe(listener);
    }
}

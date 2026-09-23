// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

// Owns access to the upstream client and applies instrumentation suppression to transport-producing operations.
internal sealed class OpAmpClientTransport : ICustomCapabilitiesSink, IDisposable
{
    private OpAmpClient? _client;

    private OpAmpClient Client => _client ?? throw new InvalidOperationException("The OpAmp client has not been prepared.");

    public void Initialize(OpAmpClient client)
    {
        if (_client != null)
        {
            throw new InvalidOperationException("The OpAmp client has already been prepared.");
        }

        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        return Client.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        await Client.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        await Client.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        Client.Subscribe(listener);
    }

    public void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        Client.Unsubscribe(listener);
    }

    public void SendEffectiveConfig(IReadOnlyCollection<EffectiveConfigFile> effectiveConfig)
    {
        InvokeWithInstrumentationSuppressed(client => client.SendEffectiveConfig(effectiveConfig));
    }

    public void SendRemoteConfigStatus(RemoteConfigStatusReport remoteConfigStatus)
    {
        InvokeWithInstrumentationSuppressed(client => client.SendRemoteConfigStatus(remoteConfigStatus));
    }

    public void SubmitCapabilities(IReadOnlyCollection<string> capabilities)
    {
        InvokeWithInstrumentationSuppressed(client => client.SendCustomCapabilities(capabilities));
    }

    public void SubmitFullState(FullStateReport report)
    {
        InvokeWithInstrumentationSuppressed(client => client.SendFullStateReport(report));
    }

    public void SendMessage(string capability, string type, ReadOnlyMemory<byte> data)
    {
        InvokeWithInstrumentationSuppressed(client => client.SendCustomMessage(capability, type, data));
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private void InvokeWithInstrumentationSuppressed(Action<OpAmpClient> operation)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        operation(Client);
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class ThrowingClientConfigurationPlugin : TestOpAmpPlugin
{
    private int _handlerDisposed;

    public int ConfigureClientCount { get; private set; }

    public bool HandlerDisposed => Volatile.Read(ref _handlerDisposed) != 0;

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.HttpClientFactory = () => new HttpClient(new DisposalTrackingHttpMessageHandler(this), disposeHandler: true);
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        ConfigureClientCount++;
        throw new InvalidOperationException("Test exception from OpAMP client configuration.");
    }

    private void SignalDisposed()
    {
        Interlocked.Exchange(ref _handlerDisposed, 1);
    }

    private sealed class DisposalTrackingHttpMessageHandler(ThrowingClientConfigurationPlugin plugin) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                plugin.SignalDisposed();
            }

            base.Dispose(disposing);
        }
    }
}

public sealed class ThrowingClientConfigurationAndDisposalPlugin : TestOpAmpPlugin
{
    private int _handlerDisposalAttempted;

    public bool HandlerDisposalAttempted => Volatile.Read(ref _handlerDisposalAttempted) != 0;

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.HttpClientFactory = () => new HttpClient(new ThrowingDisposeHttpMessageHandler(this), disposeHandler: true);
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        throw new InvalidOperationException("Test exception from OpAMP client configuration.");
    }

    private sealed class ThrowingDisposeHttpMessageHandler(ThrowingClientConfigurationAndDisposalPlugin plugin) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Interlocked.Exchange(ref plugin._handlerDisposalAttempted, 1);
                throw new InvalidOperationException("Test exception from OpAMP client disposal.");
            }
        }
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.

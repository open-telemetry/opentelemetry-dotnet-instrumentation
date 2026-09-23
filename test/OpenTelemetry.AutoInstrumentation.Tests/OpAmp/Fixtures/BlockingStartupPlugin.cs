// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class BlockingStartupPlugin : TestOpAmpPlugin
{
    private readonly List<string> _lifecycleEvents = [];
    private readonly TaskCompletionSource<bool> _requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<HttpResponseMessage> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _cancellationObserved;
    private int _customCapabilitiesRequestCount;
    private int _handlerDisposed;
    private int _requestCount;

    public int AfterStartedCount { get; private set; }

    public int BeforeStoppedCount { get; private set; }

    public bool CancellationObserved => Volatile.Read(ref _cancellationObserved) != 0;

    public int CustomCapabilitiesRequestCount => Volatile.Read(ref _customCapabilitiesRequestCount);

    public bool HandlerDisposed => Volatile.Read(ref _handlerDisposed) != 0;

    public bool HandlerWasDisposedBeforeStopCallback { get; private set; }

    public IReadOnlyCollection<string> LifecycleEvents => _lifecycleEvents.ToArray();

    public int RequestCount => Volatile.Read(ref _requestCount);

    public IOpAmpClient? Client { get; private set; }

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        _lifecycleEvents.Add("ConfigureOpAmpOptions");
        settings.HttpClientFactory = () => new HttpClient(new BlockingHttpMessageHandler(this), disposeHandler: true);
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Client = client;
        client.ReportCustomCapabilities(["com.example.first"]);
        client.ReportCustomCapabilities(["com.example.second"]);
        _lifecycleEvents.Add("ConfigureOpAmpClient");
    }

    public override void Initialized()
    {
        _lifecycleEvents.Add("Initialized");
    }

    public override void AfterOpAmpClientStarted()
    {
        AfterStartedCount++;
    }

    public override void BeforeOpAmpClientStopped()
    {
        BeforeStoppedCount++;
        HandlerWasDisposedBeforeStopCallback = HandlerDisposed;
    }

    public bool WaitForRequest(TimeSpan timeout)
    {
        return _requestStarted.Task.Wait(timeout);
    }

    public void ReleaseRequest()
    {
        _response.TrySetCanceled();
    }

    private void ObserveCancellation()
    {
        Interlocked.Exchange(ref _cancellationObserved, 1);
    }

    private void ObserveRequest(HttpRequestMessage request)
    {
        if (OpAmpTestFrameInspector.ContainsCustomCapabilities(request))
        {
            Interlocked.Increment(ref _customCapabilitiesRequestCount);
        }
    }

    private void SignalDisposed()
    {
        Interlocked.Exchange(ref _handlerDisposed, 1);
        _response.TrySetCanceled();
    }

    private sealed class BlockingHttpMessageHandler(BlockingStartupPlugin plugin) : HttpMessageHandler
    {
        private CancellationTokenRegistration _cancellationRegistration;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _cancellationRegistration = cancellationToken.Register(plugin.ObserveCancellation);
            plugin.ObserveRequest(request);
            plugin._lifecycleEvents.Add("TransportRequest");
            Interlocked.Increment(ref plugin._requestCount);
            plugin._requestStarted.TrySetResult(true);

            // Deliberately ignore cancellation until disposal to exercise the forceful fallback.
            return plugin._response.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationRegistration.Dispose();
                plugin.SignalDisposed();
            }

            base.Dispose(disposing);
        }
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.

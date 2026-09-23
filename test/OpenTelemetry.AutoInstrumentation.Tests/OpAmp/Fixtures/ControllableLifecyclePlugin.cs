// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class ControllableLifecyclePlugin : TestOpAmpPlugin
{
    private readonly ConcurrentQueue<string> _lifecycleEvents = new();
    private readonly TaskCompletionSource<bool> _postStartCallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releasePostStartCallback = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releaseResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _customCapabilitiesRequestCount;

    public bool BlockPostStartCallback { get; set; }

    public int CustomCapabilitiesRequestCount => Volatile.Read(ref _customCapabilitiesRequestCount);

    public IReadOnlyCollection<string> LifecycleEvents => _lifecycleEvents.ToArray();

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.Heartbeat.IsEnabled = false;
        settings.HttpClientFactory = () => new HttpClient(new ControllableHttpMessageHandler(this), disposeHandler: true);
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        client.ReportCustomCapabilities(["com.example.cached"]);
    }

    public override void AfterOpAmpClientStarted()
    {
        _lifecycleEvents.Enqueue("AfterStartedEntered");
        _postStartCallbackEntered.TrySetResult(true);
        if (BlockPostStartCallback)
        {
            _releasePostStartCallback.Task.GetAwaiter().GetResult();
        }

        _lifecycleEvents.Enqueue("AfterStartedExited");
    }

    public override void BeforeOpAmpClientStopped()
    {
        _lifecycleEvents.Enqueue("BeforeStopped");
    }

    public bool WaitForRequest(TimeSpan timeout)
    {
        return _requestStarted.Task.Wait(timeout);
    }

    public bool WaitForPostStartCallback(TimeSpan timeout)
    {
        return _postStartCallbackEntered.Task.Wait(timeout);
    }

    public void CompleteStartup()
    {
        _releaseResponse.TrySetResult(true);
    }

    public void ReleasePostStartCallback()
    {
        _releasePostStartCallback.TrySetResult(true);
    }

    private void ObserveRequest(HttpRequestMessage request)
    {
        if (OpAmpTestFrameInspector.ContainsCustomCapabilities(request))
        {
            Interlocked.Increment(ref _customCapabilitiesRequestCount);
        }
    }

    private sealed class ControllableHttpMessageHandler(ControllableLifecyclePlugin plugin) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            plugin.ObserveRequest(request);
            plugin._requestStarted.TrySetResult(true);
            await plugin._releaseResponse.Task.ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([]),
            };
        }
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.

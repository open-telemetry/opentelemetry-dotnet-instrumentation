// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Google.Protobuf;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class OpAmpLoaderTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LoaderShutdownTimeout = TimeSpan.FromMilliseconds(100);

    [Fact]
    public async Task UnsupportedReportingSettingsAreDisabledAndLifecycleCallbacksAreInvoked()
    {
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);

        try
        {
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();
            OpAmpLoader.StartOpAmpClient();
            await AssertCompletes(plugin.AfterStarted);

            var configuredSettings = Assert.IsType<OpAmpClientSettings>(plugin.ConfiguredSettings);
            Assert.False(configuredSettings.EffectiveConfigurationReporting.EnableReporting);
            Assert.False(configuredSettings.RemoteConfiguration.ReportsRemoteConfigStatus);
        }
        finally
        {
            OpAmpLoader.StopOpAmpClientIfRunning();
        }

        Assert.Equal(
            [
                nameof(RecordingOpAmpPlugin.ConfigureOpAmpClient),
                nameof(RecordingOpAmpPlugin.Initialized),
                nameof(RecordingOpAmpPlugin.AfterOpAmpClientStarted),
                nameof(RecordingOpAmpPlugin.BeforeOpAmpClientStopped)
            ],
            plugin.Events);
    }

    [Fact]
    public async Task ShutdownDeadlineDisposesClientDuringBlockedBeforeStopCallback()
    {
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(BlockingBeforeStopPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<BlockingBeforeStopPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        Task? loaderStopTask = null;

        try
        {
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);

            loaderStopTask = Task.Run(() => OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout));
            await AssertCompletes(plugin.BeforeStopCallbackEntered);
            await AssertCompletes(loaderStopTask);

            await AssertCompletes(plugin.HandlerDisposed);
            Assert.False(plugin.BeforeStopCallbackCompleted.IsCompleted);
        }
        finally
        {
            plugin.ReleaseBeforeStopCallback();
            if (loaderStopTask != null)
            {
                await Task.WhenAny(loaderStopTask, Task.Delay(TestTimeout));
            }

            if (plugin.BeforeStopCallbackEntered.IsCompleted)
            {
                await Task.WhenAny(plugin.BeforeStopCallbackCompleted, Task.Delay(TestTimeout));
            }
        }

        await AssertCompletes(plugin.BeforeStopCallbackCompleted);
        Assert.Equal(1, plugin.BeforeStopCallbackCount);
    }

    private static async Task AssertCompletes(Task task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(TestTimeout)).ConfigureAwait(false);
        Assert.Same(task, completedTask);
        await task.ConfigureAwait(false);
    }
}

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class RecordingOpAmpPlugin : IPlugin, IOpAmpPlugin
{
    private static readonly Guid TestInstanceUid = Guid.NewGuid();
    private static readonly byte[] ServerCapabilitiesResponse = CreateServerCapabilitiesResponse(TestInstanceUid);

    private readonly ConcurrentQueue<string> _events = new();
    private readonly TaskCompletionSource<bool> _afterStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task AfterStarted => _afterStarted.Task;

    internal OpAmpClientSettings? ConfiguredSettings { get; private set; }

    internal string[] Events => _events.ToArray();

    public void Initializing()
    {
    }

    public void Initialized()
    {
        _events.Enqueue(nameof(Initialized));

        ConfiguredSettings!.EffectiveConfigurationReporting.EnableReporting = true;
        ConfiguredSettings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
#if NET
        ArgumentNullException.ThrowIfNull(settings);
#else
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
#endif

        ConfiguredSettings = settings;
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
        settings.Heartbeat.IsEnabled = false;
        settings.InstanceUid = TestInstanceUid;
        settings.HttpClientFactory = () => new HttpClient(new RecordingHttpMessageHandler());
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
#if NET
        ArgumentNullException.ThrowIfNull(client);
#else
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }
#endif

        _events.Enqueue(nameof(ConfigureOpAmpClient));
    }

    public void AfterOpAmpClientStarted()
    {
        _events.Enqueue(nameof(AfterOpAmpClientStarted));
        _afterStarted.TrySetResult(true);
    }

    public void BeforeOpAmpClientStopped()
    {
        _events.Enqueue(nameof(BeforeOpAmpClientStopped));
    }

    private static byte[] CreateServerCapabilitiesResponse(Guid instanceUid)
    {
        using var stream = new MemoryStream();
        using var output = new CodedOutputStream(stream, leaveOpen: true);
        output.WriteTag(1, WireFormat.WireType.LengthDelimited);
        output.WriteBytes(ByteString.CopyFrom(instanceUid.ToByteArray()));
        output.WriteTag(7, WireFormat.WireType.Varint);
        output.WriteUInt64((ulong)ServerSentCapabilities.AcceptsStatus);
        output.Flush();
        return stream.ToArray();
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ServerCapabilitiesResponse),
            });
        }
    }
}

public sealed class BlockingBeforeStopPlugin : IPlugin, IOpAmpPlugin
{
    private readonly TaskCompletionSource<bool> _beforeStopCallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _beforeStopCallbackRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _beforeStopCallbackCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _handlerDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _beforeStopCallbackCount;

    public int BeforeStopCallbackCount => Volatile.Read(ref _beforeStopCallbackCount);

    internal Task BeforeStopCallbackEntered => _beforeStopCallbackEntered.Task;

    internal Task BeforeStopCallbackCompleted => _beforeStopCallbackCompleted.Task;

    internal Task HandlerDisposed => _handlerDisposed.Task;

    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
#if NET
        ArgumentNullException.ThrowIfNull(settings);
#else
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
#endif

        settings.HttpClientFactory = () => new HttpClient(new DisposalTrackingHandler(_handlerDisposed), disposeHandler: true);
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
    }

    public void AfterOpAmpClientStarted()
    {
    }

    public void BeforeOpAmpClientStopped()
    {
        Interlocked.Increment(ref _beforeStopCallbackCount);
        _beforeStopCallbackEntered.TrySetResult(true);
        _beforeStopCallbackRelease.Task.GetAwaiter().GetResult();
        _beforeStopCallbackCompleted.TrySetResult(true);
    }

    public void ReleaseBeforeStopCallback()
    {
        _beforeStopCallbackRelease.TrySetResult(true);
    }

    private sealed class DisposalTrackingHandler(TaskCompletionSource<bool> handlerDisposed) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                handlerDisposed.TrySetResult(true);
            }

            base.Dispose(disposing);
        }
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.

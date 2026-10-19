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
    private readonly OpAmpLoader _loader = new();

    [Fact]
    public async Task UnsupportedReportingSettingsAreDisabledAndLifecycleCallbacksAreInvoked()
    {
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();
            _loader.StartOpAmpClient();
            await AssertCompletes(plugin.AfterStarted);

            var configuredSettings = Assert.IsType<OpAmpClientSettings>(plugin.ConfiguredSettings);
            Assert.False(configuredSettings.EffectiveConfigurationReporting.EnableReporting);
            Assert.False(configuredSettings.RemoteConfiguration.ReportsRemoteConfigStatus);
        }
        finally
        {
            _loader.StopOpAmpClientIfRunning();
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RemoteStatusReportingRequiresOptInAndPreservesRemoteConfigAcceptance(bool reportStatus, bool acceptConfig)
    {
        // Capability masks follow AgentCapabilities in opamp.proto.
        const ulong acceptsRemoteConfigCapability = 0x00000002;
        const ulong reportsRemoteConfigCapability = 0x00001000;
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RemoteConfigStatusOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RemoteConfigStatusOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        plugin.ConfigureOptionsAction = () =>
        {
            plugin.ConfiguredSettings!.RemoteConfiguration.ReportsRemoteConfigStatus = reportStatus;
            plugin.ConfiguredSettings.RemoteConfiguration.AcceptsRemoteConfig = acceptConfig;
        };

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();
            _loader.StartOpAmpClient();
            await AssertCompletes(plugin.InitialRequestBody);

            var capabilities = ReadAgentCapabilities(await plugin.InitialRequestBody.ConfigureAwait(true));
            Assert.Equal(reportStatus, (capabilities & reportsRemoteConfigCapability) != 0);
            Assert.Equal(acceptConfig, (capabilities & acceptsRemoteConfigCapability) != 0);
        }
        finally
        {
            _loader.StopOpAmpClientIfRunning();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShutdownReturnsDuringConfigurationAndDiscardsLateClient(bool blockOptions)
    {
        using var releaseConfiguration = new ManualResetEventSlim();
        var configurationEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        Action blockConfiguration = () =>
        {
            configurationEntered.TrySetResult(true);
            releaseConfiguration.Wait();
        };
        if (blockOptions)
        {
            plugin.ConfigureOptionsAction = blockConfiguration;
        }
        else
        {
            plugin.ConfigureClientAction = blockConfiguration;
        }

        Task? prepareTask = null;
        Task? stopTask = null;
        try
        {
            prepareTask = Task.Run(() => _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager));
            await AssertCompletes(configurationEntered.Task);
            stopTask = Task.Run(() =>
            {
                _loader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
                _loader.StartOpAmpClient();
            });
            await AssertCompletes(stopTask);
            Assert.False(prepareTask.IsCompleted);

            releaseConfiguration.Set();
            await AssertCompletes(prepareTask);
            Assert.True(plugin.HandlerDisposed.IsCompleted);
            Assert.Equal(
                [nameof(RecordingOpAmpPlugin.ConfigureOpAmpClient), nameof(RecordingOpAmpPlugin.BeforeOpAmpClientStopped)],
                plugin.Events);
            Assert.False(plugin.AfterStarted.IsCompleted);
        }
        finally
        {
            releaseConfiguration.Set();
            _loader.StopOpAmpClientIfRunning();
            if (prepareTask != null)
            {
                await AssertCompletes(prepareTask);
            }

            if (stopTask != null)
            {
                await AssertCompletes(stopTask);
            }
        }
    }

    [Fact]
    public async Task ShutdownDeadlineDisposesClientDuringBlockedBeforeStopCallback()
    {
        using var releaseBeforeStop = new ManualResetEventSlim();
        var beforeStopEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var beforeStopCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        plugin.BeforeStoppedAction = () =>
        {
            beforeStopEntered.TrySetResult(true);
            releaseBeforeStop.Wait();
            beforeStopCompleted.TrySetResult(true);
        };
        Task? loaderStopTask = null;

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);

            loaderStopTask = Task.Run(() => _loader.StopOpAmpClientIfRunning(LoaderShutdownTimeout));
            await AssertCompletes(beforeStopEntered.Task);
            await AssertCompletes(loaderStopTask);

            await AssertCompletes(plugin.HandlerDisposed);
            Assert.False(beforeStopCompleted.Task.IsCompleted);
        }
        finally
        {
            releaseBeforeStop.Set();
            if (loaderStopTask != null)
            {
                await AssertCompletes(loaderStopTask);
            }

            if (beforeStopEntered.Task.IsCompleted)
            {
                await AssertCompletes(beforeStopCompleted.Task);
            }
        }

        Assert.Single(plugin.Events, name => name == nameof(RecordingOpAmpPlugin.BeforeOpAmpClientStopped));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedConfigurationCleansUpPlugin(bool throwBeforeStop)
    {
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        plugin.ConfigureClientAction = () => throw new InvalidOperationException("Plugin configuration failed.");
        if (throwBeforeStop)
        {
            plugin.BeforeStoppedAction = () => throw new InvalidOperationException("Plugin cleanup failed.");
        }

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);

            Assert.Equal(
                [
                    nameof(RecordingOpAmpPlugin.ConfigureOpAmpClient),
                    nameof(RecordingOpAmpPlugin.BeforeOpAmpClientStopped)
                ],
                plugin.Events);
            Assert.True(plugin.HandlerDisposed.IsCompleted);
        }
        finally
        {
            _loader.StopOpAmpClientIfRunning();
        }
    }

    [Fact]
    public async Task StartupReturnsDuringBlockedAfterStartCallback()
    {
        using var releaseAfterStarted = new ManualResetEventSlim();
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        plugin.AfterStartedAction = releaseAfterStarted.Wait;
        Task? loaderStartTask = null;

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            loaderStartTask = Task.Run(_loader.StartOpAmpClient);

            await AssertCompletes(plugin.AfterStarted);
            await AssertCompletes(loaderStartTask);
        }
        finally
        {
            releaseAfterStarted.Set();
            if (loaderStartTask != null)
            {
                await AssertCompletes(loaderStartTask);
            }

            _loader.StopOpAmpClientIfRunning();
        }
    }

    [Fact]
    public async Task ShutdownDeadlineIncludesBlockingHandlerDisposal()
    {
        using var releaseHandlerDisposal = new ManualResetEventSlim();
        var handlerDisposalEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(RecordingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<RecordingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        plugin.HandlerDisposalAction = () =>
        {
            handlerDisposalEntered.TrySetResult(true);
            releaseHandlerDisposal.Wait();
        };
        Task? loaderStopTask = null;

        try
        {
            _loader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            _loader.StartOpAmpClient();
            await AssertCompletes(plugin.AfterStarted);

            loaderStopTask = Task.Run(() => _loader.StopOpAmpClientIfRunning(LoaderShutdownTimeout));
            await AssertCompletes(handlerDisposalEntered.Task);
            await AssertCompletes(loaderStopTask);
            Assert.False(plugin.HandlerDisposed.IsCompleted);
        }
        finally
        {
            releaseHandlerDisposal.Set();
            if (loaderStopTask != null)
            {
                await AssertCompletes(loaderStopTask);
            }

            _loader.StopOpAmpClientIfRunning();
        }

        await AssertCompletes(plugin.HandlerDisposed);
    }

    private static ulong ReadAgentCapabilities(byte[] requestBody)
    {
        using var input = new CodedInputStream(requestBody);
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            // AgentToServer.capabilities is field 4, encoded as a uint64.
            if (tag == WireFormat.MakeTag(4, WireFormat.WireType.Varint))
            {
                return input.ReadUInt64();
            }

            input.SkipLastField();
        }

        throw new InvalidOperationException("The initial OpAMP request did not include capabilities.");
    }

    private static async Task AssertCompletes(Task task)
    {
        using var timeoutCancellation = new CancellationTokenSource();

        try
        {
            var completedTask = await Task.WhenAny(
                task,
                Task.Delay(TestTimeout, timeoutCancellation.Token))
                .ConfigureAwait(false);

            Assert.Same(task, completedTask);
            await task.ConfigureAwait(false);
        }
        finally
        {
#if NET
            await timeoutCancellation.CancelAsync().ConfigureAwait(false);
#else
            timeoutCancellation.Cancel();
#endif
        }
    }
}

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public class RecordingOpAmpPlugin : IPlugin, IOpAmpPlugin
{
    private static readonly Guid TestInstanceUid = Guid.NewGuid();
    private static readonly byte[] ServerCapabilitiesResponse = CreateServerCapabilitiesResponse(TestInstanceUid);

    private readonly ConcurrentQueue<string> _events = new();
    private readonly TaskCompletionSource<bool> _afterStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _handlerDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<byte[]> _initialRequestBody = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task AfterStarted => _afterStarted.Task;

    internal Task HandlerDisposed => _handlerDisposed.Task;

    internal Action? ConfigureOptionsAction { get; set; }

    internal Action? ConfigureClientAction { get; set; }

    internal Action? BeforeStoppedAction { get; set; }

    internal Action? AfterStartedAction { get; set; }

    internal Action? HandlerDisposalAction { get; set; }

    internal OpAmpClientSettings? ConfiguredSettings { get; private set; }

    internal Task<byte[]> InitialRequestBody => _initialRequestBody.Task;

    internal string[] Events => [.. _events];

    public void Initializing()
    {
    }

    public void Initialized()
    {
        _events.Enqueue(nameof(Initialized));
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
        settings.HttpClientFactory = () => new HttpClient(new RecordingHttpMessageHandler(this));
        ConfigureOptionsAction?.Invoke();
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
        ConfigureClientAction?.Invoke();
    }

    public void AfterOpAmpClientStarted()
    {
        _events.Enqueue(nameof(AfterOpAmpClientStarted));
        _afterStarted.TrySetResult(true);
        AfterStartedAction?.Invoke();
    }

    public void BeforeOpAmpClientStopped()
    {
        _events.Enqueue(nameof(BeforeOpAmpClientStopped));
        BeforeStoppedAction?.Invoke();
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

    private sealed class RecordingHttpMessageHandler(RecordingOpAmpPlugin plugin) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if NET
            var requestBody = await request.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
            var requestBody = await request.Content!.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            plugin._initialRequestBody.TrySetResult(requestBody);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ServerCapabilitiesResponse),
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                plugin.HandlerDisposalAction?.Invoke();
            }

            base.Dispose(disposing);
            if (disposing)
            {
                plugin._handlerDisposed.TrySetResult(true);
            }
        }
    }
}

public sealed class RemoteConfigStatusOpAmpPlugin : RecordingOpAmpPlugin, IProvideRemoteConfigStatus
{
    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        return null;
    }
}

#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.

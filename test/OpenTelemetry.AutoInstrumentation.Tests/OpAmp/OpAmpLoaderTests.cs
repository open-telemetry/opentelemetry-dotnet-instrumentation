// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class OpAmpLoaderTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LoaderShutdownTimeout = TimeSpan.FromMilliseconds(100);

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
            await AssertCompletes(plugin.BeforeStopCallbackEntered).ConfigureAwait(true);
            await AssertCompletes(loaderStopTask).ConfigureAwait(true);

            await AssertCompletes(plugin.HandlerDisposed).ConfigureAwait(true);
            Assert.False(plugin.BeforeStopCallbackCompleted.IsCompleted);
        }
        finally
        {
            plugin.ReleaseBeforeStopCallback();
            if (loaderStopTask != null)
            {
                await Task.WhenAny(loaderStopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);
            }
        }

        await AssertCompletes(plugin.BeforeStopCallbackCompleted).ConfigureAwait(true);
        Assert.Equal(1, plugin.BeforeStopCallbackCount);
    }

    private static async Task AssertCompletes(Task task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(TestTimeout)).ConfigureAwait(true);
        Assert.Same(task, completedTask);
        await task.ConfigureAwait(true);
    }
}

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
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

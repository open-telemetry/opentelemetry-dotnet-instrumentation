// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpManagerTestFactory;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpTestTimeouts;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

[Collection(OpAmpManagerTestsCollectionDefinition.Name)]
public class OpAmpManagerLifecycleTests
{
    [Fact]
    public async Task TransportStartsExplicitlyOnceAfterPluginInitialization()
    {
        var pluginManager = CreatePluginManager<BlockingStartupPlugin>();
        var plugin = GetPlugin<BlockingStartupPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);

        try
        {
            Assert.Equal(["ConfigureOpAmpOptions", "ConfigureOpAmpClient"], plugin.LifecycleEvents);
            await Assert.ThrowsAsync<InvalidOperationException>(() => plugin.Client!.FlushAsync(CancellationToken.None)).ConfigureAwait(true);

            pluginManager.Initialized();
            Assert.Equal(["ConfigureOpAmpOptions", "ConfigureOpAmpClient", "Initialized"], plugin.LifecycleEvents);

            manager.StartClient();
            manager.StartClient();

            Assert.True(plugin.WaitForRequest(TestTimeout));
            Assert.Equal(
                ["ConfigureOpAmpOptions", "ConfigureOpAmpClient", "Initialized", "TransportRequest"],
                plugin.LifecycleEvents);
            Assert.Equal(0, plugin.AfterStartedCount);
        }
        finally
        {
            plugin.ReleaseRequest();
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ShutdownClaimBeforeStartupActivationSuppressesPostStartCallback()
    {
        var pluginManager = CreatePluginManager<ControllableLifecyclePlugin>();
        var plugin = GetPlugin<ControllableLifecyclePlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        Task? stopTask = null;

        try
        {
            manager.StartClient();
            Assert.True(plugin.WaitForRequest(TestTimeout));

            stopTask = manager.StopOpAmpClientAsync();
            plugin.CompleteStartup();
            await stopTask.ConfigureAwait(true);

            Assert.Equal(["BeforeStopped"], plugin.LifecycleEvents);
            Assert.Equal(0, plugin.CustomCapabilitiesRequestCount);
        }
        finally
        {
            plugin.CompleteStartup();
            if (stopTask != null)
            {
                await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);
            }
        }
    }

    [Fact]
    public async Task StartupActivationCompletesBeforeConcurrentShutdownCallback()
    {
        var pluginManager = CreatePluginManager<ControllableLifecyclePlugin>();
        var plugin = GetPlugin<ControllableLifecyclePlugin>(pluginManager);
        plugin.BlockPostStartCallback = true;
        using var manager = CreateManager(pluginManager);
        Task? stopTask = null;

        try
        {
            manager.StartClient();
            Assert.True(plugin.WaitForRequest(TestTimeout));
            plugin.CompleteStartup();
            Assert.True(plugin.WaitForPostStartCallback(TestTimeout));

            stopTask = manager.StopOpAmpClientAsync();

            Assert.False(stopTask.IsCompleted);
            Assert.DoesNotContain("BeforeStopped", plugin.LifecycleEvents);

            plugin.ReleasePostStartCallback();
            await stopTask.ConfigureAwait(true);

            Assert.Equal(["AfterStartedEntered", "AfterStartedExited", "BeforeStopped"], plugin.LifecycleEvents);
            Assert.Equal(1, plugin.CustomCapabilitiesRequestCount);
        }
        finally
        {
            plugin.ReleasePostStartCallback();
            if (stopTask != null)
            {
                await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);
            }
        }
    }

    [Fact]
    public async Task StopRequestReturnsBeforeBlockedCancellationPropagationCompletes()
    {
        var pluginManager = CreatePluginManager<BlockingStartupPlugin>();
        var plugin = GetPlugin<BlockingStartupPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        pluginManager.Initialized();
        manager.StartClient();
        Assert.True(plugin.WaitForRequest(TestTimeout));
        var startupCancellationSourceField = typeof(OpAmpManager).GetField(
            "_startupCancellationSource",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var startupCancellationSource = Assert.IsType<CancellationTokenSource>(startupCancellationSourceField!.GetValue(manager));
        using var cancellationCallbackEntered = new ManualResetEventSlim();
        using var releaseCancellationCallback = new ManualResetEventSlim();
        var cancellationCallbackTimedOut = 0;
        using var cancellationRegistration = startupCancellationSource.Token.Register(() =>
        {
            cancellationCallbackEntered.Set();
            if (!releaseCancellationCallback.Wait(TestTimeout))
            {
                Interlocked.Exchange(ref cancellationCallbackTimedOut, 1);
            }
        });

        var stopTask = manager.StopOpAmpClientAsync();
        try
        {
            Assert.True(cancellationCallbackEntered.Wait(TestTimeout));
            Assert.Equal(0, Volatile.Read(ref cancellationCallbackTimedOut));
            Assert.False(stopTask.IsCompleted);
        }
        finally
        {
            releaseCancellationCallback.Set();
            plugin.ReleaseRequest();
        }

        await stopTask.ConfigureAwait(true);
        Assert.Equal(0, Volatile.Read(ref cancellationCallbackTimedOut));
    }
}

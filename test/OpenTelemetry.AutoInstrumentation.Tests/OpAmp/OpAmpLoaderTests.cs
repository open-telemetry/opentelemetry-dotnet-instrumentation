// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;
using OpenTelemetry.Resources;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpManagerTestFactory;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpTestTimeouts;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

[Collection(OpAmpManagerTestsCollectionDefinition.Name)]
public class OpAmpLoaderTests
{
    [Fact]
    public void DisposalFailureDuringFailedCreationDoesNotPreventReplacement()
    {
        var failingPluginManager = CreatePluginManager<ThrowingClientConfigurationAndDisposalPlugin>();
        var failingPlugin = GetPlugin<ThrowingClientConfigurationAndDisposalPlugin>(failingPluginManager);
        var replacementPluginManager = CreatePluginManager<PrimaryOpAmpPlugin>();
        var replacementPlugin = GetPlugin<PrimaryOpAmpPlugin>(replacementPluginManager);

        try
        {
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), failingPluginManager);

            Assert.True(failingPlugin.HandlerDisposalAttempted);

            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), replacementPluginManager);

            Assert.Equal(1, replacementPlugin.ConfigureClientCount);
        }
        finally
        {
            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
        }
    }

    [Fact]
    public void StartAfterShutdownDoesNotRestartTransport()
    {
        var pluginManager = CreatePluginManager<BlockingStartupPlugin>();
        var plugin = GetPlugin<BlockingStartupPlugin>(pluginManager);

        try
        {
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();

            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
            var requestCountAfterShutdown = plugin.RequestCount;
            OpAmpLoader.StartOpAmpClient();

            Assert.Equal(requestCountAfterShutdown, plugin.RequestCount);
            Assert.Equal(0, plugin.AfterStartedCount);
            Assert.Equal(1, plugin.BeforeStoppedCount);
        }
        finally
        {
            plugin.ReleaseRequest();
            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
        }
    }

    [Fact]
    public async Task ShutdownCancelsPendingStartupAndFallsBackToDisposal()
    {
        var pluginManager = CreatePluginManager<BlockingStartupPlugin>();
        var plugin = GetPlugin<BlockingStartupPlugin>(pluginManager);
        Task? stopTask = null;

        try
        {
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();
            OpAmpLoader.StartOpAmpClient();
            Assert.True(plugin.WaitForRequest(TestTimeout));

            stopTask = Task.Run(() => OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout));
            var completedTask = await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);

            Assert.Same(stopTask, completedTask);
            await stopTask.ConfigureAwait(true);
            Assert.True(plugin.CancellationObserved);
            Assert.True(plugin.HandlerDisposed);
            Assert.False(plugin.HandlerWasDisposedBeforeStopCallback);
            Assert.Equal(1, plugin.BeforeStoppedCount);
            Assert.Equal(0, plugin.AfterStartedCount);
            Assert.Equal(0, plugin.CustomCapabilitiesRequestCount);

            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);

            Assert.Equal(1, plugin.BeforeStoppedCount);
        }
        finally
        {
            plugin.ReleaseRequest();
            if (stopTask != null)
            {
                await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);
            }

            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
        }
    }

    [Fact]
    public async Task ShutdownDeadlineDoesNotWaitForBlockedProviderCallback()
    {
        var pluginManager = CreatePluginManager<BlockingEffectiveConfigPlugin>();
        var plugin = GetPlugin<BlockingEffectiveConfigPlugin>(pluginManager);
        Task? stopTask = null;

        try
        {
            plugin.ServerAcceptsEffectiveConfig = true;
            OpAmpLoader.PrepareOpAmpClient(Resource.Empty, new OpAmpSettings(), pluginManager);
            pluginManager.Initialized();
            OpAmpLoader.StartOpAmpClient();
            Assert.True(plugin.WaitForStarted(TestTimeout));

            Assert.True(plugin.WaitForRequest(TestTimeout));

            stopTask = Task.Run(() => OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout));
            var completedTask = await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);

            Assert.Same(stopTask, completedTask);
            await stopTask.ConfigureAwait(true);
            Assert.True(plugin.WaitForBeforeStopped(TestTimeout));
        }
        finally
        {
            plugin.ReleaseRequest();
            if (stopTask != null)
            {
                await Task.WhenAny(stopTask, Task.Delay(TestTimeout)).ConfigureAwait(true);
            }

            OpAmpLoader.StopOpAmpClientIfRunning(LoaderShutdownTimeout);
        }

        Assert.Equal(1, plugin.BeforeStoppedCount);
    }
}

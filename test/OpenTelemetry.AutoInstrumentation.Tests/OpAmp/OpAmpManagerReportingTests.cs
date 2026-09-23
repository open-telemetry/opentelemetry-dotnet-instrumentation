// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;
using OpenTelemetry.OpAmp.Client.Messages;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpManagerTestFactory;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpTestTimeouts;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

[Collection(OpAmpManagerTestsCollectionDefinition.Name)]
public class OpAmpManagerReportingTests
{
    [Fact]
    public async Task ProvidersAreNotInvokedWithoutExplicitOptIn()
    {
        var pluginManager = CreatePluginManager<ProviderOnlyPlugin>();
        var plugin = GetPlugin<ProviderOnlyPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        pluginManager.Initialized();
        manager.StartClient();
        Assert.True(plugin.WaitForStarted(TestTimeout));

        try
        {
            Assert.False(plugin.ObservedEffectiveConfigReporting);
            Assert.False(plugin.ObservedRemoteConfigStatusReporting);

            manager.HandleServerCapabilities(
                ServerSentCapabilities.AcceptsEffectiveConfig |
                ServerSentCapabilities.OffersRemoteConfig);
            plugin.Client!.NotifyEffectiveConfigChanged();
            plugin.Client.NotifyRemoteConfigStatusChanged();
            manager.RequestFullStateReport();
            await plugin.Client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(0, plugin.EffectiveConfigRequestCount);
            Assert.Equal(0, plugin.RemoteConfigStatusRequestCount);
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ProvidersAreNotRefreshedWithoutCurrentServerSupport()
    {
        var pluginManager = CreatePluginManager<BlockingEffectiveConfigPlugin>();
        var plugin = GetPlugin<BlockingEffectiveConfigPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        manager.StartClient();
        Assert.True(plugin.WaitForStarted(TestTimeout));

        try
        {
            plugin.Client!.NotifyEffectiveConfigChanged();
            manager.RequestFullStateReport();
            await plugin.Client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(0, plugin.RequestCount);

            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsEffectiveConfig);
            Assert.True(plugin.WaitForRequest(TestTimeout));
            plugin.ReleaseRequest();
            await plugin.Client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(1, plugin.RequestCount);
        }
        finally
        {
            plugin.ReleaseRequest();
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ProviderRefreshesDoNotBlockCallersAndPendingNotificationsAreCoalesced()
    {
        var pluginManager = CreatePluginManager<BlockingEffectiveConfigPlugin>();
        var plugin = GetPlugin<BlockingEffectiveConfigPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        manager.StartClient();
        Assert.True(plugin.WaitForStarted(TestTimeout));

        try
        {
            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsEffectiveConfig);
            Assert.True(plugin.WaitForRequest(TestTimeout));

            for (var i = 0; i < 100; i++)
            {
                plugin.Client!.NotifyEffectiveConfigChanged();
            }

            Assert.Equal(1, plugin.RequestCount);
            plugin.ReleaseRequest();
            await plugin.Client!.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(2, plugin.RequestCount);
        }
        finally
        {
            plugin.ReleaseRequest();
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task FlushCanBeCanceledWhileAnAcceptedStateRefreshIsBlocked()
    {
        var pluginManager = CreatePluginManager<BlockingEffectiveConfigPlugin>();
        var plugin = GetPlugin<BlockingEffectiveConfigPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        manager.StartClient();
        Assert.True(plugin.WaitForStarted(TestTimeout));

        manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsEffectiveConfig);

        try
        {
            Assert.True(plugin.WaitForRequest(TestTimeout));
            using var cancellationSource = new CancellationTokenSource();
            var flush = plugin.Client!.FlushAsync(cancellationSource.Token);

            Assert.False(flush.IsCompleted);
#if NET
            await cancellationSource.CancelAsync().ConfigureAwait(true);
#else
            cancellationSource.Cancel();
#endif
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flush).ConfigureAwait(true);
        }
        finally
        {
            plugin.ReleaseRequest();
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }
}

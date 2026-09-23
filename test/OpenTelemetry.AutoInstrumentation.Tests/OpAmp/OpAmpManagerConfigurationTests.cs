// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.Tests.Configurations;
using OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.Resources;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpManagerTestFactory;
using static OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures.OpAmpTestTimeouts;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

[Collection(OpAmpManagerTestsCollectionDefinition.Name)]
public class OpAmpManagerConfigurationTests
{
    [Theory]
    [InlineData(typeof(EffectiveConfigPlugin))]
    [InlineData(typeof(RemoteConfigStatusPlugin))]
    public void ProviderInterfacesDoNotEnableReportingWithoutExplicitOptIn(Type pluginType)
    {
        var pluginManager = CreatePluginManager(pluginType);
        var plugin = Assert.IsAssignableFrom<CountingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        using var manager = CreateManager(pluginManager);

        Assert.False(plugin.ConfiguredSettings!.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(plugin.ConfiguredSettings.RemoteConfiguration.ReportsRemoteConfigStatus);
    }

    [Theory]
    [InlineData(typeof(EffectiveConfigReportingPlugin), true, false)]
    [InlineData(typeof(RemoteConfigStatusReportingPlugin), false, true)]
    public void ExplicitOptInEnablesProviderBackedReporting(
        Type pluginType,
        bool reportsEffectiveConfig,
        bool reportsRemoteConfigStatus)
    {
        var pluginManager = CreatePluginManager(pluginType);
        var plugin = Assert.IsAssignableFrom<CountingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        using var manager = CreateManager(pluginManager);

        Assert.Equal(reportsEffectiveConfig, plugin.ConfiguredSettings!.EffectiveConfigurationReporting.EnableReporting);
        Assert.Equal(reportsRemoteConfigStatus, plugin.ConfiguredSettings.RemoteConfiguration.ReportsRemoteConfigStatus);
    }

    [Fact]
    public void FirstOpAmpPluginOwnsConfigurationAndProviders()
    {
        var pluginManager = CreatePluginManager(
            typeof(PrimaryOpAmpPlugin),
            typeof(ReportingOpAmpPlugin));
        var primaryPlugin = GetPlugin<PrimaryOpAmpPlugin>(pluginManager);
        var reportingPlugin = GetPlugin<ReportingOpAmpPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);

        Assert.Equal(1, primaryPlugin.ConfigureOptionsCount);
        Assert.Equal(1, primaryPlugin.ConfigureClientCount);
        Assert.NotNull(primaryPlugin.ConfiguredSettings);
        Assert.True(primaryPlugin.ConfiguredSettings.RemoteConfiguration.AcceptsRemoteConfig);
        Assert.False(primaryPlugin.ConfiguredSettings.EffectiveConfigurationReporting.EnableReporting);
        Assert.False(primaryPlugin.ConfiguredSettings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.Equal(0, reportingPlugin.ConfigureOptionsCount);
        Assert.Equal(0, reportingPlugin.ConfigureClientCount);
    }

    [Fact]
    public void ReversingOpAmpPluginOrderTransfersOwnershipAndProviderCapabilities()
    {
        var pluginManager = CreatePluginManager(
            typeof(ReportingOpAmpPlugin),
            typeof(PrimaryOpAmpPlugin));
        var reportingPlugin = GetPlugin<ReportingOpAmpPlugin>(pluginManager);
        var primaryPlugin = GetPlugin<PrimaryOpAmpPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);
        var settings = reportingPlugin.ConfiguredSettings!;
        manager.HandleServerCapabilities(
            ServerSentCapabilities.AcceptsEffectiveConfig |
            ServerSentCapabilities.OffersRemoteConfig);

        Assert.True(reportingPlugin.WaitForProviderRequests(1, TestTimeout));
        Assert.True(settings.EffectiveConfigurationReporting.EnableReporting);
        Assert.True(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        Assert.Equal(1, reportingPlugin.ConfigureOptionsCount);
        Assert.Equal(1, reportingPlugin.ConfigureClientCount);
        Assert.Equal(0, primaryPlugin.ConfigureOptionsCount);
        Assert.Equal(0, primaryPlugin.ConfigureClientCount);
    }

    [Fact]
    public void AppliesConfiguredCustomMessageLimitsBeforePluginConfiguration()
    {
        const int maxPendingCustomMessages = 123;
        const int maxPendingCustomMessageBytes = 456;
        var pluginManager = CreatePluginManager<CustomMessageLimitsPlugin>();
        var plugin = GetPlugin<CustomMessageLimitsPlugin>(pluginManager);
        var opAmpSettings = new OpAmpSettings();
        opAmpSettings.LoadEnvVar(new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { ConfigurationKeys.OpAmpMaxPendingCustomMessages, "123" },
            { ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "456" }
        })));
        using var manager = CreateManager(pluginManager, opAmpSettings);
        var clientSettings = plugin.ConfiguredSettings!;

        Assert.Equal(maxPendingCustomMessages, plugin.ObservedMaxPendingCustomMessages);
        Assert.Equal(maxPendingCustomMessageBytes, plugin.ObservedMaxPendingCustomMessageBytes);
        Assert.Equal(CustomMessageLimitsPlugin.OverrideMaxPendingCustomMessages, clientSettings.MaxPendingCustomMessages);
        Assert.Equal(CustomMessageLimitsPlugin.OverrideMaxPendingCustomMessageBytes, clientSettings.MaxPendingCustomMessageBytes);
    }

    [Fact]
    public void PluginFacingClientDoesNotExposeManagerLifetime()
    {
        var pluginManager = CreatePluginManager<PrimaryOpAmpPlugin>();
        var plugin = GetPlugin<PrimaryOpAmpPlugin>(pluginManager);
        using var manager = CreateManager(pluginManager);

        Assert.NotNull(plugin.Client);
        Assert.False(plugin.Client is IDisposable);
    }

    [Fact]
    public void ClientConfigurationFailureReturnsNoManagerAndDisposesClient()
    {
        var pluginManager = CreatePluginManager<ThrowingClientConfigurationPlugin>();
        var plugin = GetPlugin<ThrowingClientConfigurationPlugin>(pluginManager);

        Assert.False(OpAmpManager.TryCreate(Resource.Empty, new OpAmpSettings(), pluginManager, out _));

        Assert.Equal(1, plugin.ConfigureClientCount);
        Assert.True(plugin.HandlerDisposed);
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class OpAmpSettingsTests
{
    [Fact]
    public void LoadEnvVar_CustomMessageLimits()
    {
        var configuration = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { ConfigurationKeys.OpAmpMaxPendingCustomMessages, "4096" },
            { ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "134217728" },
        }));
        var settings = new OpAmpSettings();

        settings.LoadEnvVar(configuration);

        Assert.Equal(4096, settings.MaxPendingCustomMessages);
        Assert.Equal(134217728, settings.MaxPendingCustomMessageBytes);
    }

    [Fact]
    public void LoadEnvVar_CustomMessageLimitsAreUnsetByDefault()
    {
        var configuration = new Configuration(false, new NameValueConfigurationSource(false, []));
        var settings = new OpAmpSettings();

        settings.LoadEnvVar(configuration);

        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingCustomMessageBytes);
    }

    [Theory]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessages, "0")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessages, "-1")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "0")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "-1")]
    public void LoadEnvVar_NonPositiveCustomMessageLimitIsIgnored(string key, string invalidValue)
    {
        var configuration = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { key, invalidValue },
        }));
        var settings = new OpAmpSettings();

        settings.LoadEnvVar(configuration);

        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingCustomMessageBytes);
    }

    [Theory]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessages, "0")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessages, "-1")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "0")]
    [InlineData(ConfigurationKeys.OpAmpMaxPendingCustomMessageBytes, "-1")]
    public void LoadEnvVar_NonPositiveCustomMessageLimitFailsFast(string key, string invalidValue)
    {
        var configuration = new Configuration(true, new NameValueConfigurationSource(true, new NameValueCollection
        {
            { key, invalidValue },
        }));
        var settings = new OpAmpSettings();

        Assert.Throws<InvalidOperationException>(() => settings.LoadEnvVar(configuration));
    }
}

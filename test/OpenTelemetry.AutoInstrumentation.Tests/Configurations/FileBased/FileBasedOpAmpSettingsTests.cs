// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FileBasedOpAmpSettingsTests
{
    [Fact]
    public void LoadFile_GeneralSettings()
    {
        var serverUrl = "wss://localhost:4320/v1/opamp";
        const int maxPendingCustomMessages = 4096;
        const int maxPendingCustomMessageBytes = 134217728;
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration()
            {
                ServerUrl = serverUrl,
                MaxPendingCustomMessages = maxPendingCustomMessages,
                MaxPendingCustomMessageBytes = maxPendingCustomMessageBytes,
            }
        };

        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.True(settings.OpAmpClientEnabled);
        Assert.Equal(new Uri(serverUrl, UriKind.Absolute), settings.ServerUrl);
        Assert.Equal(maxPendingCustomMessages, settings.MaxPendingCustomMessages);
        Assert.Equal(maxPendingCustomMessageBytes, settings.MaxPendingCustomMessageBytes);
    }

    [Fact]
    public void LoadFile_CustomMessageLimitsAreUnsetByDefault()
    {
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration(),
        };
        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingCustomMessageBytes);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    public void LoadFile_NonPositiveCustomMessageLimitIsIgnored(bool setMaxPendingCustomMessages, int invalidValue)
    {
        var opAmpConfiguration = CreateOpAmpConfigurationWithInvalidLimit(setMaxPendingCustomMessages, invalidValue);
        var conf = new YamlConfiguration
        {
            OpAmp = opAmpConfiguration,
        };
        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingCustomMessageBytes);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    public void LoadFile_NonPositiveCustomMessageLimitFailsFast(bool setMaxPendingCustomMessages, int invalidValue)
    {
        var opAmpConfiguration = CreateOpAmpConfigurationWithInvalidLimit(setMaxPendingCustomMessages, invalidValue);
        var conf = new YamlConfiguration
        {
            FailFast = true,
            OpAmp = opAmpConfiguration,
        };
        var settings = new OpAmpSettings();

        Assert.Throws<InvalidOperationException>(() => settings.LoadFile(conf));
    }

    private static OpAmpConfiguration CreateOpAmpConfigurationWithInvalidLimit(bool setMaxPendingCustomMessages, int invalidValue)
    {
        return new OpAmpConfiguration
        {
            MaxPendingCustomMessages = setMaxPendingCustomMessages ? invalidValue : null,
            MaxPendingCustomMessageBytes = setMaxPendingCustomMessages ? null : invalidValue,
        };
    }
}

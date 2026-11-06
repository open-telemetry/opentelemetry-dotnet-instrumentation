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
        const int maxPendingMessageBytes = 134217728;
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration()
            {
                ServerUrl = serverUrl,
                MaxPendingCustomMessages = maxPendingCustomMessages,
                MaxPendingMessageBytes = maxPendingMessageBytes,
            }
        };

        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.True(settings.OpAmpClientEnabled);
        Assert.Equal(new Uri(serverUrl, UriKind.Absolute), settings.ServerUrl);
        Assert.Equal(maxPendingCustomMessages, settings.MaxPendingCustomMessages);
        Assert.Equal(maxPendingMessageBytes, settings.MaxPendingMessageBytes);
    }

    [Fact]
    public void LoadFile_EmptyOpAmpConfiguration_ShouldEnableOpAmpWithDefaultSettings()
    {
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration(),
        };
        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.True(settings.OpAmpClientEnabled);
        Assert.Null(settings.ServerUrl);
        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingMessageBytes);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    public void LoadFile_NonPositiveMessageLimitIsIgnored(int? maxPendingCustomMessages, int? maxPendingMessageBytes)
    {
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration
            {
                MaxPendingCustomMessages = maxPendingCustomMessages,
                MaxPendingMessageBytes = maxPendingMessageBytes,
            },
        };
        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingMessageBytes);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    public void LoadFile_NonPositiveMessageLimitFailsFast(int? maxPendingCustomMessages, int? maxPendingMessageBytes)
    {
        var conf = new YamlConfiguration
        {
            FailFast = true,
            OpAmp = new OpAmpConfiguration
            {
                MaxPendingCustomMessages = maxPendingCustomMessages,
                MaxPendingMessageBytes = maxPendingMessageBytes,
            },
        };
        var settings = new OpAmpSettings();

        Assert.Throws<InvalidOperationException>(() => settings.LoadFile(conf));
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FileBasedOpAmpSettingsTests
{
    [Fact]
    public void LoadFile_GeneralSettings()
    {
        var serverUrl = "wss://localhost:4320/v1/opamp";
        var conf = new YamlConfiguration
        {
            OpAmp = new OpAmpConfiguration()
            {
                ServerUrl = serverUrl
            }
        };

        var settings = new OpAmpSettings();

        settings.LoadFile(conf);

        Assert.True(settings.OpAmpClientEnabled);
        Assert.Equal(new Uri(serverUrl, UriKind.Absolute), settings.ServerUrl);
    }
}

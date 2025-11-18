// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedGeneralSettingsTests
{
    [Fact]
    public void LoadFile_GeneralSettings()
    {
        var conf = new YamlConfiguration
        {
            Disabled = false,
            FlushOnUnhandledException = true,
        };

        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        Assert.True(settings.SetupSdk);
        Assert.True(settings.FlushOnUnhandledException);
    }

    [Fact]
    public void LoadFile_FailFastSettings()
    {
        var conf = new YamlConfiguration
        {
            FailFast = true,
        };

        var settings = new FailFastSettings();

        settings.LoadFile(conf);

        Assert.True(settings.FailFast);
    }
}

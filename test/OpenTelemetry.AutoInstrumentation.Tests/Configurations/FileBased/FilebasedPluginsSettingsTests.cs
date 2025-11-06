// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedPluginsSettingsTests
{
    [Fact]
    public void LoadFile_PluginsSettings()
    {
        var conf = new YamlConfiguration
        {
            Plugins = new PluginsConfiguration
            {
                Plugins = ["Test.Plugins.Plugin, Test.Plugins"]
            }
        };

        var settings = new PluginsSettings();

        settings.LoadFile(conf);

        Assert.Single(settings.Plugins);
        Assert.Contains("Test.Plugins.Plugin, Test.Plugins", settings.Plugins);
    }

    [Fact]
    public void LoadFile_PluginsListSettings()
    {
        var conf = new YamlConfiguration
        {
            Plugins = new PluginsConfiguration
            {
                PluginsList = "Test.Plugins.Plugin, Test.Plugins"
            }
        };

        var settings = new PluginsSettings();

        settings.LoadFile(conf);
        Assert.Single(settings.Plugins);
        Assert.Contains("Test.Plugins.Plugin, Test.Plugins", settings.Plugins);
    }

    [Fact]
    public void LoadFile_MergePluginsSettings()
    {
        var conf = new YamlConfiguration
        {
            Plugins = new PluginsConfiguration
            {
                Plugins = ["Test1.Plugins.Plugin, Test1.Plugins", "Test2.Plugins.Plugin, Test2.Plugins"],
                PluginsList = "Test2.Plugins.Plugin, Test2.Plugins:Test3.Plugins.Plugin, Test3.Plugins"
            }
        };

        var settings = new PluginsSettings();

        settings.LoadFile(conf);

        Assert.Equal(3, settings.Plugins.Count);
        Assert.Contains("Test1.Plugins.Plugin, Test1.Plugins", settings.Plugins);
        Assert.Contains("Test2.Plugins.Plugin, Test2.Plugins", settings.Plugins);
        Assert.Contains("Test3.Plugins.Plugin, Test3.Plugins", settings.Plugins);
    }
}

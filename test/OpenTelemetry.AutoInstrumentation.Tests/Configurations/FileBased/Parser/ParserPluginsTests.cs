// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserPluginsTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestPluginsFile.yaml");

        Assert.NotNull(config);

        Assert.NotNull(config.Plugins);
        Assert.NotNull(config.Plugins.Plugins);
        Assert.Null(config.Plugins.PluginsList);
        Assert.Equal(2, config.Plugins.Plugins.Count);
        Assert.Equal("Test1.Plugins.Plugin, Test1.Plugins", config.Plugins.Plugins[0]);
        Assert.Equal("Test2.Plugins.Plugin, Test2.Plugins", config.Plugins.Plugins[1]);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "Test.Plugins.Plugin, Test.Plugins");

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestPluginsFileEnvVars.yaml");

        Assert.NotNull(config);

        Assert.NotNull(config.Plugins);
        Assert.Null(config.Plugins.Plugins);
        Assert.NotNull(config.Plugins.PluginsList);

        Assert.Equal("Test.Plugins.Plugin, Test.Plugins", config.Plugins.PluginsList);
    }
}

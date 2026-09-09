// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserOpAmpTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestOpAmpFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);
        Assert.NotNull(config.OpAmp);
        Assert.Equal("wss://localhost:4320/v1/opamp", config.OpAmp?.ServerUrl);
        Assert.Equal(4096, config.OpAmp?.MaxPendingCustomMessages);
        Assert.Equal(134217728, config.OpAmp?.MaxPendingCustomMessageBytes);
    }

    [Fact]
    public void Parse_EmptyConfigYaml_ShouldPopulateDefaultsCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestEmptyFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);

        // By default opamp is disabled
        Assert.Null(config.OpAmp);
    }

    [Theory]
    [InlineData("file_format: \"1.0\"\nopamp/development:\n")]
    [InlineData("file_format: \"1.0\"\nopamp/development: {}\n")]
    public void Parse_EmptyOpAmpConfigYaml_ShouldEnableOpAmpWithDefaultSettings(string yaml)
    {
        var config = YamlParser.ParseYamlContent<YamlConfiguration>(yaml);

        Assert.NotNull(config);
        Assert.NotNull(config.OpAmp);
        Assert.Null(config.OpAmp.ServerUrl);
        Assert.Null(config.OpAmp.MaxPendingCustomMessages);
        Assert.Null(config.OpAmp.MaxPendingCustomMessageBytes);

        var settings = new OpAmpSettings();
        settings.LoadFile(config);

        Assert.True(settings.OpAmpClientEnabled);
        Assert.Null(settings.ServerUrl);
        Assert.Null(settings.MaxPendingCustomMessages);
        Assert.Null(settings.MaxPendingCustomMessageBytes);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", "wss://localhost:4320/v1/opamp" },
            { "OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGES", "4096" },
            { "OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGE_BYTES", "134217728" },
        });

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestOpAmpFileEnvVars.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);
        Assert.NotNull(config.OpAmp);
        Assert.Equal("wss://localhost:4320/v1/opamp", config.OpAmp?.ServerUrl);
        Assert.Equal(4096, config.OpAmp?.MaxPendingCustomMessages);
        Assert.Equal(134217728, config.OpAmp?.MaxPendingCustomMessageBytes);
    }
}

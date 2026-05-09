// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Xunit;
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
        Assert.Equal("wss://localhost:4318/v1/opamp", config.OpAmp?.ServerUrl);
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

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", "wss://localhost:4318/v1/opamp" }
        });

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestOpAmpFileEnvVars.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);
        Assert.NotNull(config.OpAmp);
        Assert.Equal("wss://localhost:4318/v1/opamp", config.OpAmp?.ServerUrl);
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserGeneralTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestGeneralFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0-rc.1", config.FileFormat);
        Assert.False(config.Disabled);
        Assert.False(config.FailFast);
        Assert.False(config.FlushOnUnhandledException);
        Assert.False(config.NetFxRedirectEnabled);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "false");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "false");

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestGeneralFileEnvVars.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0-rc.1", config.FileFormat);
        Assert.True(config.Disabled);
        Assert.True(config.FailFast);
        Assert.False(config.FlushOnUnhandledException);
        Assert.False(config.NetFxRedirectEnabled);
    }
}

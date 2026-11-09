// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Vendors.YamlDotNet.Core;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserGeneralTests
{
    [Theory]
    [InlineData("file_format: []")]
    [InlineData("file_format: {}")]
    [InlineData("disabled: [true]")]
    [InlineData("disabled: {value: true}")]
    [InlineData("opamp/development:\n  max_pending_custom_messages: []")]
    [InlineData("opamp/development:\n  max_pending_custom_messages: {}")]
    public void Parse_NonScalarValueForScalarField_ShouldThrowYamlException(string yaml)
    {
        var exception = Assert.Throws<YamlException>(() => YamlParser.ParseYamlContent<YamlConfiguration>(yaml));

        Assert.Contains("Scalar", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ObjectCollections_ShouldPreserveStructureAndExpandEnvVars()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_COLLECTION_VALUE", "expanded" }
        });

        const string yaml = """
            values:
              - key: ${YAMLPARSER_TESTS_COLLECTION_VALUE}
            """;

        var config = YamlParser.ParseYamlContent<Dictionary<string, object>>(yaml);

        var sequence = Assert.IsType<List<object>>(config["values"]);
        var mapping = Assert.IsType<Dictionary<object, object>>(Assert.Single(sequence));
        Assert.Equal("expanded", mapping["key"]);
    }

    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestGeneralFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);
        Assert.False(config.Disabled);
        Assert.False(config.FailFast);
        Assert.False(config.FlushOnUnhandledException);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_SDK_DISABLED", "true" },
            { "OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true" },
            { "OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "false" },
        });

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestGeneralFileEnvVars.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0", config.FileFormat);
        Assert.True(config.Disabled);
        Assert.True(config.FailFast);
        Assert.False(config.FlushOnUnhandledException);
    }
}

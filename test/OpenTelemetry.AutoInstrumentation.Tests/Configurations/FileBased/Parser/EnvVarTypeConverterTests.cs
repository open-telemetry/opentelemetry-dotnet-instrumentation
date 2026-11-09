// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Core.Events;
using YamlParser = Vendors.YamlDotNet.Core.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class EnvVarTypeConverterTests
{
    private readonly EnvVarTypeConverter _converter = new();

    [Fact]
    public void ReadYaml_StringWithEnvVar_ReplacesCorrectly()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_TEST_ENV", "HelloWorld" }
        });

        var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_TEST_ENV}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal("HelloWorld", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ReadYaml_WithFallback_UsesFallbackIfEnvNullOrEmpty(string? envValue)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_NOT_EXIST_ENV", envValue }
        });

        var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_NOT_EXIST_ENV:-FallbackValue}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal("FallbackValue", result);
    }

    [Theory]
    [InlineData("RealValue")]
    [InlineData(" ")]
    public void ReadYaml_WithEnvVarPresent_IgnoresFallback(string envValue)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_MY_ENV", envValue }
        });

        var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_MY_ENV:-FallbackValue}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal(envValue, result);
    }

    [Theory]
    [InlineData(null, "${YAMLPARSER_TESTS_UNKNOWN_ENV}", "")]
    [InlineData("", "${YAMLPARSER_TESTS_UNKNOWN_ENV}", "")]
    [InlineData(null, "${YAMLPARSER_TESTS_UNKNOWN_ENV:-}", "")]
    [InlineData("", "${YAMLPARSER_TESTS_UNKNOWN_ENV:-}", "")]
    [InlineData(null, "prefix-${YAMLPARSER_TESTS_UNKNOWN_ENV}-suffix", "prefix--suffix")]
    public void ReadYaml_EnvVarNullOrEmpty_ReplacesWithEmptyValue(string? envValue, string yaml, string expected)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_UNKNOWN_ENV", envValue }
        });

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "${YAMLPARSER_TESTS_NUMBER_ENV}", typeof(int?), null)]
    [InlineData("", "${YAMLPARSER_TESTS_NUMBER_ENV}", typeof(int?), null)]
    [InlineData(null, "${YAMLPARSER_TESTS_NUMBER_ENV:-0.5}", typeof(double), 0.5)]
    [InlineData("", "${YAMLPARSER_TESTS_NUMBER_ENV:-0.5}", typeof(double), 0.5)]
    [InlineData("0.25", "${YAMLPARSER_TESTS_NUMBER_ENV:-0.5}", typeof(double), 0.25)]
    public void ReadYaml_NumericEnvVar_SubstitutesBeforeParsing(string? envValue, string yaml, Type type, object? expected)
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "YAMLPARSER_TESTS_NUMBER_ENV", envValue }
        });

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, type, _ => null);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0", typeof(int), 0)]
    [InlineData("-1", typeof(long), -1L)]
    [InlineData("0.0", typeof(float), 0.0f)]
    [InlineData("-1.0", typeof(double), -1.0)]
    [InlineData("false", typeof(bool), false)]
    [InlineData("\"\"", typeof(string), "")]
    [InlineData("\"\"", typeof(int?), null)]
    [InlineData("\"\"", typeof(uint?), null)]
    public void ReadYaml_ParsesPrimitives(string yaml, Type type, object? expected)
    {
        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, type, _ => null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReadYaml_InvalidFormat_ThrowsFormatException()
    {
        var parser = new YamlParser(new StringReader("notANumber"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        Assert.Throws<FormatException>(() => _converter.ReadYaml(parser, typeof(int), _ => null));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    public void ReadYaml_NonScalar_ThrowsYamlException(string yaml)
    {
        var parser = new YamlParser(new StringReader(yaml));
        parser.Consume<StreamStart>();
        parser.Consume<DocumentStart>();

        Assert.Throws<YamlException>(() => _converter.ReadYaml(parser, typeof(string), _ => null));
    }
}

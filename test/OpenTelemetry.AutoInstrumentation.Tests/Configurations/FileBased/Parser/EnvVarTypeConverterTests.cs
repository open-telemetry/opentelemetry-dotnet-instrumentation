// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Xunit;
using YamlParser = Vendors.YamlDotNet.Core.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

public class EnvVarTypeConverterTests
{
    private readonly EnvVarTypeConverter _converter = new();

    [Fact]
    public void ReadYaml_StringWithEnvVar_ReplacesCorrectly()
    {
        try
        {
            Environment.SetEnvironmentVariable("YAMLPARSER_TESTS_TEST_ENV", "HelloWorld");

            var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_TEST_ENV}"));
            FileBasedTestHelper.MoveParserToScalar(parser);

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("HelloWorld", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("YAMLPARSER_TESTS_TEST_ENV", null);
        }
    }

    [Fact]
    public void ReadYaml_WithFallback_UsesFallbackIfEnvNotSet()
    {
        var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_NOT_EXIST_ENV:-FallbackValue}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal("FallbackValue", result);
    }

    [Fact]
    public void ReadYaml_WithEnvVarPresent_IgnoresFallback()
    {
        try
        {
            Environment.SetEnvironmentVariable("YAMLPARSER_TESTS_MY_ENV", "RealValue");

            var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_MY_ENV:-FallbackValue}"));
            FileBasedTestHelper.MoveParserToScalar(parser);

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("RealValue", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("YAMLPARSER_TESTS_MY_ENV", null);
        }
    }

    [Fact]
    public void ReadYaml_UnknownEnvVarWithoutFallback_KeepsOriginalPattern()
    {
        var parser = new YamlParser(new StringReader("${YAMLPARSER_TESTS_UNKNOWN_ENV}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = _converter.ReadYaml(parser, typeof(string), _ => null);

        Assert.Equal("${YAMLPARSER_TESTS_UNKNOWN_ENV}", result);
    }

    [Theory]
    [InlineData("0", typeof(int), 0)]
    [InlineData("-1", typeof(long), -1L)]
    [InlineData("0.0", typeof(float), 0.0f)]
    [InlineData("-1.0", typeof(double), -1.0)]
    [InlineData("false", typeof(bool), false)]
    [InlineData("", typeof(string), null)]
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
}

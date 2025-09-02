// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Xunit;
using YamlParser = Vendors.YamlDotNet.Core.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBasedConfiguration.Parser;

public class EnvVarTypeConverterTests
{
    private readonly EnvVarTypeConverter _converter = new();

    [Fact]
    public void ReadYaml_StringWithEnvVar_ReplacesCorrectly()
    {
        try
        {
            Environment.SetEnvironmentVariable("TEST_ENV", "HelloWorld");

            var parser = new YamlParser(new StringReader("${TEST_ENV}"));
            parser.MoveNext(); // StreamStart
            parser.MoveNext(); // DocumentStart
            parser.MoveNext(); // Scalar

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("HelloWorld", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_ENV", null);
        }
    }

    [Fact]
    public void ReadYaml_WithFallback_UsesFallbackIfEnvNotSet()
    {
        try
        {
            Environment.SetEnvironmentVariable("NOT_EXIST", null);

            var parser = new YamlParser(new StringReader("${NOT_EXIST:-FallbackValue}"));
            parser.MoveNext();
            parser.MoveNext();
            parser.MoveNext();

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("FallbackValue", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOT_EXIST", null);
        }
    }

    [Fact]
    public void ReadYaml_WithEnvVarPresent_IgnoresFallback()
    {
        try
        {
            Environment.SetEnvironmentVariable("MY_ENV", "RealValue");

            var parser = new YamlParser(new StringReader("${MY_ENV:-FallbackValue}"));
            parser.MoveNext();
            parser.MoveNext();
            parser.MoveNext();

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("RealValue", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MY_ENV", null);
        }
    }

    [Fact]
    public void ReadYaml_UnknownEnvVarWithoutFallback_KeepsOriginalPattern()
    {
        try
        {
            Environment.SetEnvironmentVariable("UNKNOWN_ENV", null);

            var parser = new YamlParser(new StringReader("${UNKNOWN_ENV}"));
            parser.MoveNext();
            parser.MoveNext();
            parser.MoveNext();

            var result = _converter.ReadYaml(parser, typeof(string), _ => null);

            Assert.Equal("${UNKNOWN_ENV}", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("UNKNOWN_ENV", null);
        }
    }

    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("123", typeof(long), 123L)]
    [InlineData("3.14", typeof(float), 3.14f)]
    [InlineData("2.718", typeof(double), 2.718)]
    [InlineData("true", typeof(bool), true)]
    public void ReadYaml_ParsesPrimitives(string yaml, Type type, object expected)
    {
        var parser = new YamlParser(new StringReader(yaml));
        parser.MoveNext();
        parser.MoveNext();
        parser.MoveNext();

        var result = _converter.ReadYaml(parser, type, _ => null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReadYaml_InvalidFormat_ThrowsFormatException()
    {
        var parser = new YamlParser(new StringReader("notANumber"));
        parser.MoveNext();
        parser.MoveNext();
        parser.MoveNext();

        Assert.Throws<FormatException>(() => _converter.ReadYaml(parser, typeof(int), _ => null));
    }
}

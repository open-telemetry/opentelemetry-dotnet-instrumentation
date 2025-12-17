// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Serialization;
using Xunit;
using YamlParser = Vendors.YamlDotNet.Core.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

public class ConditionalDeserializerTests
{
    [Fact]
    public void WithoutAttribute_DelegatesToInnerDeserializer()
    {
        var inner = new DummyDeserializer { ReturnValue = new UnmarkedClass() };
        var sut = new ConditionalDeserializer(inner);

        var parser = new YamlParser(new StringReader("value"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(UnmarkedClass), (_, _) => null, out var value, _ => null);

        Assert.True(result);
        Assert.True(inner.Called);
        Assert.IsType<UnmarkedClass>(value);
    }

    [Fact]
    public void WithAttribute_EmptyScalar_CreatesDefaultInstance()
    {
        var inner = new DummyDeserializer();
        var sut = new ConditionalDeserializer(inner);

        var yaml = "\"\"";

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.True(result);
        Assert.IsType<MarkedClass>(value);
        Assert.False(inner.Called);
    }

    [Fact]
    public void WithAttribute_MappingStart_UsesInnerDeserializer()
    {
        var inner = new DummyDeserializer { ReturnValue = null };
        var sut = new ConditionalDeserializer(inner);

        var parser = new YamlParser(new StringReader("{}"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.True(result);
        Assert.True(inner.Called);
        Assert.NotNull(value);
        Assert.IsType<MarkedClass>(value);
    }

    [Fact]
    public void WithAttribute_NeitherScalarNorMapping_ReturnsFalse()
    {
        var inner = new DummyDeserializer();
        var sut = new ConditionalDeserializer(inner);

        var parser = new YamlParser(new StringReader("[]"));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.False(result);
        Assert.Null(value);
        Assert.False(inner.Called);
    }

    private class DummyDeserializer : INodeDeserializer
    {
        public bool Called { get; private set; }

        public object? ReturnValue { get; set; }

        public bool Deserialize(
            IParser reader,
            Type expectedType,
            Func<IParser, Type, object?> nestedObjectDeserializer,
            out object? value,
            ObjectDeserializer rootDeserializer)
        {
            Called = true;
            value = ReturnValue;
            return true;
        }
    }

    [EmptyObjectOnEmptyYaml]
    private class MarkedClass
    {
    }

    private class UnmarkedClass
    {
    }
}

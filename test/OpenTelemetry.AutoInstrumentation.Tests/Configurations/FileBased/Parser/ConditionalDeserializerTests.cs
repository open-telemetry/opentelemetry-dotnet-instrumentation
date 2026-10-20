// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Serialization;
using Vendors.YamlDotNet.Serialization.NodeDeserializers;
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

    [Theory]
    [InlineData("---")]
    [InlineData("null")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("~")]
    public void WithAttribute_NullScalar_CreatesDefaultInstance(string yaml)
    {
        var sut = new ConditionalDeserializer(new NullNodeDeserializer());

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.True(result);
        Assert.IsType<MarkedClass>(value);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("''")]
    public void WithAttribute_EmptyStringScalar_IsNotHandledAsNull(string yaml)
    {
        var sut = new ConditionalDeserializer(new NullNodeDeserializer());

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.False(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public void WithAttribute_NonNullNode_IsNotHandledAsNull(string yaml)
    {
        var sut = new ConditionalDeserializer(new NullNodeDeserializer());

        var parser = new YamlParser(new StringReader(yaml));
        FileBasedTestHelper.MoveParserToScalar(parser);

        var result = sut.Deserialize(parser, typeof(MarkedClass), (_, _) => null, out var value, _ => null);

        Assert.False(result);
        Assert.Null(value);
    }

    private sealed class DummyDeserializer : INodeDeserializer
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
#pragma warning disable CA1812 //  Avoid uninstantiated internal classes. Used in tests by Yaml deserializer.
    private sealed class MarkedClass
#pragma warning restore CA1812 //  Avoid uninstantiated internal classes. Used in tests by Yaml deserializer.
    {
    }

    private sealed class UnmarkedClass
    {
    }
}

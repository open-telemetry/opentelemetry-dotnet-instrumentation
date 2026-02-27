// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

public class CelParserTests
{
    [Fact]
    public void Parse_Identifier_ReturnsIdentifierNode()
    {
        var tokens = CelLexer.Tokenize("instance");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("\"hello\"")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void Parse_Literal_ReturnsLiteralNode(string expression)
    {
        var tokens = CelLexer.Tokenize(expression);
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_MemberAccess_ReturnsMemberAccessNode()
    {
        var tokens = CelLexer.Tokenize("instance.Name");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_IndexAccess_ReturnsIndexAccessNode()
    {
        var tokens = CelLexer.Tokenize("arguments[0]");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_FunctionCall_ReturnsFunctionCallNode()
    {
        var tokens = CelLexer.Tokenize("concat(\"a\", \"b\")");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Theory]
    [InlineData("x == y")]
    [InlineData("x != y")]
    [InlineData("x < y")]
    [InlineData("x > y")]
    [InlineData("x <= y")]
    [InlineData("x >= y")]
    [InlineData("x && y")]
    [InlineData("x || y")]
    public void Parse_BinaryOperator_ReturnsBinaryOperatorNode(string expression)
    {
        var tokens = CelLexer.Tokenize(expression);
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Theory]
    [InlineData("!x")]
    [InlineData("-x")]
    public void Parse_UnaryOperator_ReturnsUnaryOperatorNode(string expression)
    {
        var tokens = CelLexer.Tokenize(expression);
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_TernaryOperator_ReturnsTernaryNode()
    {
        var tokens = CelLexer.Tokenize("x ? y : z");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_ChainedMemberAccess_ReturnsNestedMemberAccessNodes()
    {
        var tokens = CelLexer.Tokenize("arguments[0].Customer.Name");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Fact]
    public void Parse_ComplexExpression_Success()
    {
        var tokens = CelLexer.Tokenize("arguments[0].Value > 10 && return.Success == true ? \"pass\" : \"fail\"");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
    }

    [Theory]
    [InlineData("x +")]
    [InlineData("+ x")]
    [InlineData("x ==")]
    [InlineData("(x")]
    [InlineData("x)")]
    [InlineData("[0]")]
    [InlineData(".Name")]
    public void Parse_InvalidSyntax_ThrowsException(string expression)
    {
        var tokens = CelLexer.Tokenize(expression);
        var parser = new CelParser(tokens);

        Assert.Throws<InvalidOperationException>(() => parser.Parse());
    }

    [Theory]
    [InlineData("10 * 5")]
    [InlineData("20 / 4")]
    [InlineData("17 % 5")]
    [InlineData("2 * 3 * 4")]
    public void Parse_MultiplicativeExpression_ReturnsCorrectNode(string input)
    {
        var tokens = CelLexer.Tokenize(input);
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
        Assert.IsType<CelBinaryOperatorNode>(node);
    }

    [Theory]
    [InlineData("10 + 5 * 2")]
    [InlineData("20 - 4 / 2")]
    public void Parse_MixedAdditiveAndMultiplicative_ReturnsCorrectNode(string input)
    {
        var tokens = CelLexer.Tokenize(input);
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
        Assert.IsType<CelBinaryOperatorNode>(node);
    }

    [Fact]
    public void Parse_UnaryNegation_ReturnsUnaryOperatorNode()
    {
        var tokens = CelLexer.Tokenize("-(5)");
        var parser = new CelParser(tokens);

        var node = parser.Parse();

        Assert.NotNull(node);
        Assert.IsType<CelUnaryOperatorNode>(node);
    }
}

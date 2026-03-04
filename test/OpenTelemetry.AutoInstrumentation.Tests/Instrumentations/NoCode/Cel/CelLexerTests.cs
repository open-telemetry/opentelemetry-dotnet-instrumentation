// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

public class CelLexerTests
{
    [Theory]
    [InlineData("instance")]
    [InlineData("arguments")]
    [InlineData("return")]
    [InlineData("method")]
    [InlineData("type")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void Tokenize_Keywords_ReturnsCorrectTokens(string input)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count); // Token + EOF
        Assert.Equal(input, tokens[0].Value);
    }

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("'world'", "world")]
    [InlineData("\"hello world\"", "hello world")]
    [InlineData("\"with\\\"quotes\"", "with\"quotes")]
    [InlineData("'with\\'quotes'", "with'quotes")]
    [InlineData("\"with\\nnewline\"", "with\nnewline")]
    [InlineData("\"with\\rcarriage\"", "with\rcarriage")]
    [InlineData("\"with\\ttab\"", "with\ttab")]
    [InlineData("\"with\\\\backslash\"", "with\\backslash")]
    [InlineData("\"all\\n\\r\\t\\\\\\'\\\"escapes\"", "all\n\r\t\\'\"escapes")]
    public void Tokenize_StringLiterals_ReturnsCorrectValues(string input, string expectedValue)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Theory]
    [InlineData("123", "123")]
    [InlineData("0", "0")]
    [InlineData("999999", "999999")]
    [InlineData("123.45", "123.45")]
    [InlineData("0.5", "0.5")]
    [InlineData("99.99", "99.99")]
    public void Tokenize_Numbers_ReturnsCorrectValues(string input, string expectedValue)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Theory]
    [InlineData("==")]
    [InlineData("!=")]
    [InlineData("<=")]
    [InlineData(">=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("&&")]
    [InlineData("||")]
    [InlineData("!")]
    [InlineData("+")]
    [InlineData("-")]
    public void Tokenize_Operators_ReturnsCorrectTokens(string input)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count); // Operator + EOF
    }

    [Theory]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("?")]
    [InlineData(":")]
    public void Tokenize_Punctuation_ReturnsCorrectTokens(string input)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count); // Punctuation + EOF
    }

    [Fact]
    public void Tokenize_ComplexExpression_ReturnsAllTokens()
    {
        var tokens = CelLexer.Tokenize("arguments[0].Name == \"test\"");

        Assert.Equal(9, tokens.Count); // arguments [ 0 ] . Name == "test" EOF
        Assert.Equal("arguments", tokens[0].Value);
        Assert.Equal("0", tokens[2].Value);
        Assert.Equal("Name", tokens[5].Value);
        Assert.Equal("test", tokens[7].Value);
    }

    [Fact]
    public void Tokenize_WithWhitespace_IgnoresWhitespace()
    {
        var tokens = CelLexer.Tokenize("  arguments [ 0 ]  ");

        Assert.Equal(5, tokens.Count); // arguments [ 0 ] EOF
        Assert.Equal("arguments", tokens[0].Value);
        Assert.Equal("0", tokens[2].Value);
    }

    [Fact]
    public void Tokenize_FunctionCall_ReturnsCorrectTokens()
    {
        var tokens = CelLexer.Tokenize("concat(\"hello\", \"world\")");

        Assert.Equal(7, tokens.Count); // concat ( "hello" , "world" ) EOF
        Assert.Equal("concat", tokens[0].Value);
        Assert.Equal("hello", tokens[2].Value);
        Assert.Equal("world", tokens[4].Value);
    }

    [Fact]
    public void Tokenize_TernaryExpression_ReturnsCorrectTokens()
    {
        var tokens = CelLexer.Tokenize("x > 10 ? \"high\" : \"low\"");

        Assert.Equal(8, tokens.Count);
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsOnlyEOF()
    {
        var tokens = CelLexer.Tokenize(string.Empty);

        Assert.Single(tokens);
    }

    [Theory]
    [InlineData("myVar", "myVar")]
    [InlineData("_private", "_private")]
    [InlineData("var123", "var123")]
    [InlineData("camelCase", "camelCase")]
    [InlineData("PascalCase", "PascalCase")]
    public void Tokenize_Identifiers_ReturnsCorrectValues(string input, string expectedValue)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("%")]
    public void Tokenize_MultiplicativeOperators_ReturnsCorrectTokens(string input)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count); // Operator + EOF
        Assert.Equal(input, tokens[0].Value);
    }

    [Theory]
    [InlineData("\"with\\rcarriagereturn\"", "with\rcarriagereturn")]
    [InlineData("'single\\nquotes'", "single\nquotes")]
    public void Tokenize_StringLiterals_WithAdditionalEscapes_ReturnsCorrectValues(string input, string expectedValue)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => CelLexer.Tokenize("@"));
    }

    [Fact]
    public void Tokenize_UnterminatedString_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => CelLexer.Tokenize("\"unterminated"));
    }

    [Theory]
    [InlineData("-5", 2)] // Negative number: -5, EOF
    [InlineData("(-5)", 4)] // Left paren, negative number, right paren, EOF
    [InlineData("10-5", 4)] // 10, minus operator, 5, EOF
    [InlineData("10 - 5", 4)] // 10, minus operator, 5, EOF
    [InlineData("x--5", 4)] // x, minus operator, negative number, EOF
    [InlineData("x- -5", 4)] // x, minus operator, negative number, EOF
    [InlineData("5+-3", 4)] // 5, plus operator, negative number, EOF
    [InlineData("5*-3", 4)] // 5, multiply operator, negative number, EOF
    public void Tokenize_NegativeNumberVsMinusOperator_ReturnsCorrectTokenCount(string input, int expectedCount)
    {
        var tokens = CelLexer.Tokenize(input);

        Assert.Equal(expectedCount, tokens.Count);
    }

    [Fact]
    public void Tokenize_NegativeNumberAfterOperator_ParsesAsNegativeNumber()
    {
        var tokens = CelLexer.Tokenize("x+-5");

        Assert.Equal(4, tokens.Count);
        Assert.Equal("x", tokens[0].Value);
        Assert.Equal(CelTokenType.Identifier, tokens[0].Type);
        Assert.Equal("+", tokens[1].Value);
        Assert.Equal(CelTokenType.Plus, tokens[1].Type);
        Assert.Equal("-5", tokens[2].Value);
        Assert.Equal(CelTokenType.Number, tokens[2].Type);
    }

    [Fact]
    public void Tokenize_MinusOperatorBetweenNumbers_ParsesAsOperator()
    {
        var tokens = CelLexer.Tokenize("10-5");

        Assert.Equal(4, tokens.Count);
        Assert.Equal("10", tokens[0].Value);
        Assert.Equal(CelTokenType.Number, tokens[0].Type);
        Assert.Equal("-", tokens[1].Value);
        Assert.Equal(CelTokenType.Minus, tokens[1].Type);
        Assert.Equal("5", tokens[2].Value);
        Assert.Equal(CelTokenType.Number, tokens[2].Type);
    }

    [Theory]
    [InlineData("\"invalid\\s\"", "Invalid escape sequence '\\s'")]
    [InlineData("\"invalid\\x\"", "Invalid escape sequence '\\x'")]
    [InlineData("\"invalid\\z\"", "Invalid escape sequence '\\z'")]
    [InlineData("\"invalid\\a\"", "Invalid escape sequence '\\a'")]
    [InlineData("\"invalid\\b\"", "Invalid escape sequence '\\b'")]
    [InlineData("'invalid\\s'", "Invalid escape sequence '\\s'")]
    public void Tokenize_InvalidEscapeSequence_ThrowsException(string input, string expectedMessageFragment)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CelLexer.Tokenize(input));
        Assert.Contains(expectedMessageFragment, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"trailing\\")]
    [InlineData("'trailing\\")]
    public void Tokenize_BackslashAtEndOfString_ThrowsException(string input)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CelLexer.Tokenize(input));
        Assert.Contains("Invalid escape sequence at end of string", exception.Message, StringComparison.Ordinal);
    }
}

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
    [InlineData("\"with\\nnewline\"", "with\nnewline")]
    [InlineData("\"with\\ttab\"", "with\ttab")]
    [InlineData("\"with\\\\backslash\"", "with\\backslash")]
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
}

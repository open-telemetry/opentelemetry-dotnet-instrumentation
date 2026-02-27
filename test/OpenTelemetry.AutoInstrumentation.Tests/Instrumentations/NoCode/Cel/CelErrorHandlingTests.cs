// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

public class CelErrorHandlingTests
{
    [Fact]
    public void Evaluate_NullInstance_PropertyAccess_ReturnsNull()
    {
        var expr = CelExpression.Parse("instance.Name");
        var context = CreateContext(instance: null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NullArguments_IndexAccess_ReturnsNull()
    {
        var expr = CelExpression.Parse("arguments[0]");
        var context = CreateContext(arguments: null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_IndexOutOfRange_ReturnsNull()
    {
        var expr = CelExpression.Parse("arguments[100]");
        var context = CreateContext(arguments: new object?[] { "a", "b" });

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NegativeIndex_ReturnsNull()
    {
        var expr = CelExpression.Parse("arguments[-1]");
        var context = CreateContext(arguments: new object?[] { "a", "b", "c" });

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NonExistentProperty_ReturnsNull()
    {
        var expr = CelExpression.Parse("instance.NonExistentProperty");
        var instance = new TestClass { Name = "test" };
        var context = CreateContext(instance: instance);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_PropertyAccessOnNull_ReturnsNull()
    {
        var expr = CelExpression.Parse("arguments[0].Nested.Value");
        var obj = new TestClass { Nested = null };
        var context = CreateContext(arguments: new object?[] { obj });

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NullComparison_HandlesCorrectly()
    {
        var expr = CelExpression.Parse("arguments[0] == null");
        var context = CreateContext(arguments: new object?[] { null });

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_SubstringOutOfRange_ReturnsEmpty()
    {
        var expr = CelExpression.Parse("substring(\"hello\", 100)");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SizeOnNull_ReturnsNull()
    {
        var expr = CelExpression.Parse("size(null)");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_InvalidSyntax_ReturnsNull()
    {
        var result = CelExpression.Parse("this is not valid CEL");

        Assert.Null(result);
    }

    [Fact]
    public void Parse_UnmatchedParentheses_ReturnsNull()
    {
        var result = CelExpression.Parse("(arguments[0]");

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_StringWithEscapedCharacters_HandlesCorrectly()
    {
        var expr = CelExpression.Parse("\"line1\\nline2\\ttab\"");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("line1\nline2\ttab", result);
    }

    private static NoCodeExpressionContext CreateContext(
        object? instance = null,
        object?[]? arguments = null,
        object? returnValue = null)
    {
        return new NoCodeExpressionContext(instance, arguments, returnValue, null, null);
    }

    private sealed class TestClass
    {
        public string? Name { get; set; }

        public TestClass? Nested { get; set; }
    }
}

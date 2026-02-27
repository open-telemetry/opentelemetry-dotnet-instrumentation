// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

/// <summary>
/// Tests for operator precedence in CEL expressions.
/// </summary>
public class CelOperatorPrecedenceTests
{
    [Fact]
    public void Precedence_AndBeforeOr()
    {
        // true || false && false should be evaluated as: true || (false && false) = true
        var expr = CelExpression.Parse("true || false && false");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Precedence_MultipleAndOr()
    {
        // false && false || true should be: (false && false) || true = true
        var expr = CelExpression.Parse("false && false || true");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Precedence_ComparisonBeforeLogicalAnd()
    {
        // 5 > 3 && 2 < 4 should be: (5 > 3) && (2 < 4) = true && true = true
        var expr = CelExpression.Parse("5 > 3 && 2 < 4");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Precedence_NotBeforeAnd()
    {
        // !false && true should be: (!false) && true = true && true = true
        var expr = CelExpression.Parse("!false && true");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Parentheses_OverrideAndOrPrecedence()
    {
        // (true || false) && false should be: true && false = false
        var expr = CelExpression.Parse("(true || false) && false");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Parentheses_NestedParentheses()
    {
        // ((true || false) && true) || false = (true && true) || false = true
        var expr = CelExpression.Parse("((true || false) && true) || false");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Precedence_TernaryLowestPrecedence()
    {
        // true && false ? "a" : "b" should be: (true && false) ? "a" : "b" = "b"
        var expr = CelExpression.Parse("true && false ? \"a\" : \"b\"");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("b", result);
    }

    [Fact]
    public void Complex_MultipleOperatorsWithoutParentheses()
    {
        // 5 > 3 && 2 < 4 || false && true
        // Should be: ((5 > 3) && (2 < 4)) || (false && true) = true
        var expr = CelExpression.Parse("5 > 3 && 2 < 4 || false && true");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void MemberAccess_InComparison()
    {
        var expr = CelExpression.Parse("arguments[0].Value > 10 && arguments[0].Value < 20");
        var obj = new TestClass { Value = 15 };
        var context = CreateContext(arguments: new object?[] { obj });

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void FunctionCall_InComparison()
    {
        var expr = CelExpression.Parse("size(arguments[0]) > 2");
        var array = new[] { "a", "b", "c", "d" };
        var context = CreateContext(arguments: new object?[] { array });

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    private static NoCodeExpressionContext CreateContext(
        object? instance = null,
        object?[]? arguments = null)
    {
        return new NoCodeExpressionContext(instance, arguments, null, null, null);
    }

    private sealed class TestClass
    {
        public int Value { get; set; }
    }
}

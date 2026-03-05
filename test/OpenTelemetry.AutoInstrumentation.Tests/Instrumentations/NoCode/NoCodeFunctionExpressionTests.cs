// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode;

public class NoCodeFunctionExpressionTests
{
    [Fact]
    public void Parse_IsNullFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("isnull($return)");

        Assert.NotNull(result);
        Assert.Equal("isnull", result.FunctionName);
        Assert.Single(result.Arguments);
    }

    [Fact]
    public void Parse_EqualsFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("equals($return.Status, \"error\")");

        Assert.NotNull(result);
        Assert.Equal("equals", result.FunctionName);
        Assert.Equal(2, result.Arguments.Length);
    }

    [Fact]
    public void Parse_InvalidFunction_ReturnsNull()
    {
        var result = NoCodeFunctionExpression.Parse("unknownfunction($arg1)");

        Assert.Null(result);
    }

    [Fact]
    public void Parse_NotAFunction_ReturnsNull()
    {
        var result = NoCodeFunctionExpression.Parse("$arg1.Property");

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_IsNull_ReturnsTrueForNull()
    {
        var expr = NoCodeFunctionExpression.Parse("isnull($return)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_IsNull_ReturnsFalseForNonNull()
    {
        var expr = NoCodeFunctionExpression.Parse("isnull($return)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: "value",
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_IsNotNull_ReturnsTrueForNonNull()
    {
        var expr = NoCodeFunctionExpression.Parse("isnotnull($return)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: "value",
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_Equals_ReturnsTrueForEqualValues()
    {
        var expr = NoCodeFunctionExpression.Parse("equals($arg1, \"expected\")");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: ["expected"],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_Equals_ReturnsFalseForDifferentValues()
    {
        var expr = NoCodeFunctionExpression.Parse("equals($arg1, \"expected\")");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: ["different"],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_NotEquals_ReturnsTrueForDifferentValues()
    {
        var expr = NoCodeFunctionExpression.Parse("notequals($arg1, 0)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [42],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_Substring_ExtractsSubstring()
    {
        var expr = NoCodeFunctionExpression.Parse("substring($arg1, 0, 5)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: ["hello world"],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Evaluate_ToString_ConvertsToString()
    {
        var expr = NoCodeFunctionExpression.Parse("tostring($arg1)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [12345],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("12345", result);
    }

    [Fact]
    public void Evaluate_NestedFunctions()
    {
        var expr = NoCodeFunctionExpression.Parse("substring(tostring($arg1), 0, 2)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [12345],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("12", result);
    }
}

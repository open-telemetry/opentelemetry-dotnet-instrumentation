// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode;

public class NoCodeFunctionExpressionTests
{
    [Fact]
    public void Parse_ConcatFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("concat($arg1, \"-\", $arg2)");

        Assert.NotNull(result);
        Assert.Equal("CONCAT", result.FunctionName);
        Assert.Equal(3, result.Arguments.Length);
    }

    [Fact]
    public void Parse_CoalesceFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("coalesce($arg1.Name, \"default\")");

        Assert.NotNull(result);
        Assert.Equal("COALESCE", result.FunctionName);
        Assert.Equal(2, result.Arguments.Length);
    }

    [Fact]
    public void Parse_IsNullFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("isnull($return)");

        Assert.NotNull(result);
        Assert.Equal("ISNULL", result.FunctionName);
        Assert.Single(result.Arguments);
    }

    [Fact]
    public void Parse_EqualsFunction_ReturnsValidExpression()
    {
        var result = NoCodeFunctionExpression.Parse("equals($return.Status, \"error\")");

        Assert.NotNull(result);
        Assert.Equal("EQUALS", result.FunctionName);
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
    public void Evaluate_Concat_CombinesStrings()
    {
        var expr = NoCodeFunctionExpression.Parse("concat($arg1, \"-\", $arg2)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: ["hello", "world"],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Evaluate_Concat_WithProperties()
    {
        var expr = NoCodeFunctionExpression.Parse("concat($arg1.Name, \":\", $arg1.Value)");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [new TestClass { Name = "key", Value = "data" }],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("key:data", result);
    }

    [Fact]
    public void Evaluate_Coalesce_ReturnsFirstNonNull()
    {
        var expr = NoCodeFunctionExpression.Parse("coalesce($arg1, $arg2, \"default\")");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [null, "second"],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("second", result);
    }

    [Fact]
    public void Evaluate_Coalesce_ReturnsDefault()
    {
        var expr = NoCodeFunctionExpression.Parse("coalesce($arg1, $arg2, \"default\")");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [null, null],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("default", result);
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
        var expr = NoCodeFunctionExpression.Parse("concat(tostring($arg1), \"-\", coalesce($arg2, \"none\"))");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [123, null],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expr!.Evaluate(context);

        Assert.Equal("123-none", result);
    }

    private sealed class TestClass
    {
        public string? Name { get; set; }

        public string? Value { get; set; }
    }
}

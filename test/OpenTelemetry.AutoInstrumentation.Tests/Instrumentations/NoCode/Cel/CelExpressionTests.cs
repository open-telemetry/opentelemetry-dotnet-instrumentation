// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

public class CelExpressionTests
{
    [Theory]
    [InlineData("instance")]
    [InlineData("arguments")]
    [InlineData("return")]
    [InlineData("method")]
    [InlineData("type")]
    public void Parse_Identifiers_Success(string expression)
    {
        var result = CelExpression.Parse(expression);

        Assert.NotNull(result);
        Assert.Equal(expression, result.RawExpression);
    }

    [Theory]
    [InlineData("\"hello\"")]
    [InlineData("'world'")]
    [InlineData("123")]
    [InlineData("123.45")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void Parse_Literals_Success(string expression)
    {
        var result = CelExpression.Parse(expression);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("arguments[0]")]
    [InlineData("arguments[1].Name")]
    [InlineData("instance.Property")]
    [InlineData("return.Value")]
    public void Parse_MemberAccess_Success(string expression)
    {
        var result = CelExpression.Parse(expression);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrNull_ReturnsNull(string? expression)
    {
        var result = CelExpression.Parse(expression);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_InstanceIdentifier_ReturnsInstance()
    {
        var expr = CelExpression.Parse("instance");
        var instance = new TestClass { Name = "test" };
        var context = CreateContext(instance: instance);

        var result = expr!.Evaluate(context);

        Assert.Same(instance, result);
    }

    [Fact]
    public void Evaluate_ArgumentsIdentifier_ReturnsArgumentsArray()
    {
        var expr = CelExpression.Parse("arguments");
        var args = new object?[] { "a", "b", "c" };
        var context = CreateContext(arguments: args);

        var result = expr!.Evaluate(context);

        Assert.Same(args, result);
    }

    [Fact]
    public void Evaluate_ReturnIdentifier_ReturnsReturnValue()
    {
        var expr = CelExpression.Parse("return");
        var returnValue = new TestClass { Name = "result" };
        var context = CreateContext(returnValue: returnValue);

        var result = expr!.Evaluate(context);

        Assert.Same(returnValue, result);
    }

    [Fact]
    public void Evaluate_MethodIdentifier_ReturnsMethodName()
    {
        var expr = CelExpression.Parse("method");
        var context = CreateContext(methodName: "TestMethod");

        var result = expr!.Evaluate(context);

        Assert.Equal("TestMethod", result);
    }

    [Fact]
    public void Evaluate_TypeIdentifier_ReturnsTypeName()
    {
        var expr = CelExpression.Parse("type");
        var context = CreateContext(typeName: "TestType");

        var result = expr!.Evaluate(context);

        Assert.Equal("TestType", result);
    }

    [Fact]
    public void Evaluate_StringLiteral_ReturnsString()
    {
        var expr = CelExpression.Parse("\"hello world\"");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Evaluate_NumberLiteral_ReturnsNumber()
    {
        var expr = CelExpression.Parse("123.45");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(123.45, result);
    }

    [Fact]
    public void Evaluate_IndexAccess_ReturnsElement()
    {
        var expr = CelExpression.Parse("arguments[1]");
        var context = CreateContext(arguments: new object?[] { "a", "b", "c" });

        var result = expr!.Evaluate(context);

        Assert.Equal("b", result);
    }

    [Fact]
    public void Evaluate_PropertyAccess_ReturnsPropertyValue()
    {
        var expr = CelExpression.Parse("instance.Name");
        var instance = new TestClass { Name = "TestName" };
        var context = CreateContext(instance: instance);

        var result = expr!.Evaluate(context);

        Assert.Equal("TestName", result);
    }

    [Fact]
    public void Evaluate_NestedPropertyAccess_ReturnsNestedValue()
    {
        var expr = CelExpression.Parse("arguments[0].Nested.Name");
        var nested = new TestClass { Name = "NestedValue" };
        var obj = new TestClassWithNested { Nested = nested };
        var context = CreateContext(arguments: new object?[] { obj });

        var result = expr!.Evaluate(context);

        Assert.Equal("NestedValue", result);
    }

    [Theory]
    [InlineData("1 == 1", true)]
    [InlineData("1 == 2", false)]
    [InlineData("\"a\" == \"a\"", true)]
    [InlineData("\"a\" == \"b\"", false)]
    public void Evaluate_Equality_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1 < 2", true)]
    [InlineData("2 < 1", false)]
    [InlineData("1 <= 1", true)]
    [InlineData("2 > 1", true)]
    [InlineData("1 >= 1", true)]
    public void Evaluate_Comparison_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true && true", true)]
    [InlineData("true && false", false)]
    [InlineData("false && true", false)]
    public void Evaluate_LogicalAnd_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true || false", true)]
    [InlineData("false || true", true)]
    [InlineData("false || false", false)]
    public void Evaluate_LogicalOr_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    public void Evaluate_LogicalNot_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("!0", true)]
    [InlineData("!1", false)]
    [InlineData("!-1", false)]
    [InlineData("!123", false)]
    public void Evaluate_LogicalNot_WithIntegers_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_LogicalNot_WithNull_ReturnsTrue()
    {
        var expr = CelExpression.Parse("!null");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Theory]
    [InlineData("!\"\"", true)]
    [InlineData("!\"hello\"", false)]
    [InlineData("!\" \"", false)]
    public void Evaluate_LogicalNot_WithStrings_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_LogicalNot_WithNonBooleanObject_ReturnsFalse()
    {
        // Testing the default case in CelUnaryOperatorNode.IsTrue which returns true for objects
        var instance = new TestClass { Name = "test" };
        var expr = CelExpression.Parse("!instance");
        var context = CreateContext(instance: instance);

        var result = expr!.Evaluate(context);

        // Any non-null object returns true in IsTrue, so !instance returns false
        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_LogicalNot_WithLong_ReturnsCorrectResult()
    {
        // Testing long type specifically for CelUnaryOperatorNode.IsTrue
        var instance = new { Value = 0L };
        var expr = CelExpression.Parse("!instance.Value");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result); // 0L is falsy, so !0L is true
    }

    [Theory]
    [InlineData("true ? \"yes\" : \"no\"", "yes")]
    [InlineData("false ? \"yes\" : \"no\"", "no")]
    public void Evaluate_Ternary_ReturnsCorrectBranch(string expression, string expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_Concat_ConcatenatesStrings()
    {
        var expr = CelExpression.Parse("concat(\"hello\", \" \", \"world\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Evaluate_Coalesce_ReturnsFirstNonNull()
    {
        var expr = CelExpression.Parse("coalesce(null, null, \"found\", \"ignored\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("found", result);
    }

    [Fact]
    public void Evaluate_Substring_ReturnsSubstring()
    {
        var expr = CelExpression.Parse("substring(\"hello world\", 6)");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("world", result);
    }

    [Fact]
    public void Evaluate_String_ConvertsToString()
    {
        var expr = CelExpression.Parse("string(123)");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal("123", result);
    }

    [Fact]
    public void Evaluate_Size_StringLength_ReturnsLength()
    {
        var expr = CelExpression.Parse("size(\"hello\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(5, result);
    }

    [Fact]
    public void Evaluate_StartsWith_True()
    {
        var expr = CelExpression.Parse("startsWith(\"hello world\", \"hello\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_EndsWith_True()
    {
        var expr = CelExpression.Parse("endsWith(\"hello world\", \"world\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_Contains_True()
    {
        var expr = CelExpression.Parse("contains(\"hello world\", \"lo wo\")");
        var context = CreateContext();

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_ComplexExpression_WithMixedOperators()
    {
        var expr = CelExpression.Parse("arguments[0] != null && arguments[0].Value > 10");
        var obj = new TestClassWithValue { Value = 15 };
        var context = CreateContext(arguments: new object?[] { obj });

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Evaluate_ComplexExpression_WithFunctions()
    {
        var expr = CelExpression.Parse("concat(\"Status: \", return.Success ? \"OK\" : \"Failed\")");
        var returnValue = new TestClassWithSuccess { Success = true };
        var context = CreateContext(returnValue: returnValue);

        var result = expr!.Evaluate(context);

        Assert.Equal("Status: OK", result);
    }

    [Theory]
    [InlineData("-5", -5)]
    [InlineData("-42", -42)]
    [InlineData("-(10 + 5)", -15)]
    public void Evaluate_UnaryNegation_ReturnsNegativeValue(string expression, int expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("-5.5", -5.5)]
    [InlineData("-3.14", -3.14)]
    [InlineData("-(10.5 + 5.5)", -16.0)]
    public void Evaluate_UnaryNegation_WithDouble_ReturnsNegativeValue(string expression, double expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture), 5);
    }

    [Fact]
    public void Evaluate_UnaryNegation_WithNonNumeric_ReturnsNull()
    {
        var expr = CelExpression.Parse("-\"hello\"");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_UnaryNegation_WithLong_ReturnsNegativeValue()
    {
        var instance = new { Value = 100L };
        var expr = CelExpression.Parse("-instance.Value");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(-100L, result);
    }

    [Fact]
    public void Evaluate_UnaryNegation_WithFloat_ReturnsNegativeValue()
    {
        var instance = new { Value = 5.5f };
        var expr = CelExpression.Parse("-instance.Value");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(-5.5f, result);
    }

    [Fact]
    public void Evaluate_UnaryNegation_WithDecimal_ReturnsNegativeValue()
    {
        var instance = new { Value = 99.99m };
        var expr = CelExpression.Parse("-instance.Value");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(-99.99m, result);
    }

    [Theory]
    [InlineData("10 * 5", 50)]
    [InlineData("20 / 4", 5)]
    [InlineData("17 % 5", 2)]
    [InlineData("2 * 3 * 4", 24)]
    [InlineData("100 / 10 / 2", 5)]
    public void Evaluate_MultiplicativeOperators_ReturnsCorrectResult(string expression, int expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("5 + 3", 8)]
    [InlineData("10.5 + 5.5", 16.0)]
    public void Evaluate_AddOperator_WithNumericTypes_ReturnsSum(string expression, double expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture), 5);
    }

    [Fact]
    public void Evaluate_AddOperator_WithNonNumericTypes_ReturnsNull()
    {
        var expr = CelExpression.Parse("\"hello\" + \"world\"");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_MultiplyOperator_WithNonNumericTypes_ReturnsNull()
    {
        var expr = CelExpression.Parse("\"hello\" * 3");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("10 - 5", 5)]
    [InlineData("100 - 50", 50)]
    [InlineData("0 - 10", -10)]
    [InlineData("5 - 5", 0)]
    [InlineData("20 - 10 - 5", 5)]
    public void Evaluate_SubtractOperator_WithIntegers_ReturnsCorrectResult(string expression, int expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("10.5 - 5.2", 5.3)]
    [InlineData("100.0 - 50.5", 49.5)]
    [InlineData("0.0 - 10.5", -10.5)]
    public void Evaluate_SubtractOperator_WithDoubles_ReturnsCorrectResult(string expression, double expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture), 5);
    }

    [Fact]
    public void Evaluate_SubtractOperator_WithNull_ReturnsNull()
    {
        var expr1 = CelExpression.Parse("null - 5");
        var expr2 = CelExpression.Parse("10 - null");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result1 = expr1!.Evaluate(context);
        var result2 = expr2!.Evaluate(context);

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public void Evaluate_DivideOperator_WithNonNumericTypes_ReturnsNull()
    {
        var expr = CelExpression.Parse("\"hello\" / 2");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ModuloOperator_WithNonNumericTypes_ReturnsNull()
    {
        var expr = CelExpression.Parse("\"hello\" % 2");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_SubtractOperator_WithNonNumericTypes_ReturnsNull()
    {
        var expr = CelExpression.Parse("\"hello\" - \"world\"");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("true ? 1 : 2", 1)]
    [InlineData("false ? 1 : 2", 2)]
    [InlineData("10 > 5 ? \"yes\" : \"no\"", "yes")]
    [InlineData("10 < 5 ? \"yes\" : \"no\"", "no")]
    [InlineData("null ? \"yes\" : \"no\"", "no")]
    [InlineData("0 ? \"yes\" : \"no\"", "no")]
    [InlineData("1 ? \"yes\" : \"no\"", "yes")]
    public void Evaluate_TernaryOperator_WithVariousConditions_ReturnsCorrectBranch(string expression, object expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_TernaryOperator_WithLongCondition_ReturnsCorrectBranch()
    {
        // Testing long type which is not covered in the basic tests
        var instance = new { Value = 100L };
        var expr = CelExpression.Parse("instance.Value > 50 ? \"large\" : \"small\"");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal("large", result);
    }

    [Fact]
    public void Evaluate_TernaryOperator_WithObject_ReturnsTrueBranch()
    {
        // Testing default case where object is truthy
        var instance = new TestClass { Name = "test" };
        var expr = CelExpression.Parse("instance ? \"has instance\" : \"no instance\"");
        var context = CreateContext(instance: instance);

        var result = expr!.Evaluate(context);

        Assert.Equal("has instance", result);
    }

    [Theory]
    [InlineData("10 > 5 && 20 > 10", true)]
    [InlineData("10 > 5 && 20 < 10", false)]
    [InlineData("null && true", false)]
    [InlineData("0 && true", false)]
    [InlineData("1 && true", true)]
    [InlineData("10 > 5 || 20 < 10", true)]
    [InlineData("10 < 5 || 20 < 10", false)]
    [InlineData("null || true", true)]
    [InlineData("0 || true", true)]
    [InlineData("false || false", false)]
    public void Evaluate_LogicalOperators_WithDifferentTypes_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_LogicalAnd_WithEmptyString_ReturnsFalse()
    {
        var expr = CelExpression.Parse("\"\" && true");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_LogicalOr_WithNonEmptyString_ReturnsTrue()
    {
        var expr = CelExpression.Parse("\"hello\" || false");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Theory]
    [InlineData("0.0 && true", false)]
    [InlineData("0.1 && true", true)]
    [InlineData("0.0 || true", true)]
    [InlineData("-0.5 && true", true)]
    public void Evaluate_LogicalOperators_WithFloatingPoint_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10.5 > 5.5", true)]
    [InlineData("3.14 < 2.71", false)]
    [InlineData("10.0 > 10", false)]
    [InlineData("5.5 < 10.1", true)]
    public void Evaluate_ComparisonOperators_WithDoubles_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"apple\" < \"banana\"", true)]
    [InlineData("\"zebra\" > \"apple\"", true)]
    [InlineData("\"abc\" < \"abc\"", false)]
    [InlineData("\"hello\" > \"world\"", false)]
    [InlineData("\"world\" > \"hello\"", true)]
    public void Evaluate_ComparisonOperators_WithStrings_ReturnsCorrectResult(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("substring(\"hello\", 0, 10)", "hello")]
    [InlineData("substring(\"hello\", 5, 10)", "")]
    [InlineData("substring(\"hello\", 1)", "ello")]
    [InlineData("substring(\"hello\", 10)", "")]
    public void Evaluate_SubstringFunction_WithEdgeCases_ReturnsCorrectResult(string expression, string expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("contains(\"hello\", null)", false)]
    [InlineData("startsWith(\"hello\", null)", false)]
    [InlineData("endsWith(\"hello\", null)", false)]
    public void Evaluate_StringFunctions_WithNullArgument_ReturnsFalse(string expression, bool expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_SizeFunction_WithNonCollectionNonString_ReturnsNull()
    {
        var instance = new { Value = 42 };
        var expr = CelExpression.Parse("size(instance)");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_SizeFunction_WithArray_ReturnsLength()
    {
        var args = new object[] { "a", "b", "c", "d" };
        var expr = CelExpression.Parse("size(arguments)");
        var context = new NoCodeExpressionContext(null, args, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(4, result);
    }

    [Fact]
    public void Evaluate_StringFunction_WithInvalidArgumentCount_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("string()");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SubstringFunction_WithInvalidArgumentCount_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("substring(\"hello\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SubstringFunction_WithNegativeStart_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("substring(\"hello\", -1)");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SubstringFunction_WithNegativeLength_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("substring(\"hello\", 1, -1)");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SubstringFunction_WithNonIntStart_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("substring(\"hello\", \"not an int\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_SubstringFunction_WithNonIntLength_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("substring(\"hello\", 1, \"not an int\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Evaluate_StartsWithFunction_WithInvalidArgumentCount_ReturnsFalse()
    {
        var expr = CelExpression.Parse("startsWith(\"hello\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_EndsWithFunction_WithInvalidArgumentCount_ReturnsFalse()
    {
        var expr = CelExpression.Parse("endsWith(\"hello\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_ContainsFunction_WithInvalidArgumentCount_ReturnsFalse()
    {
        var expr = CelExpression.Parse("contains(\"hello\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Evaluate_SizeFunction_WithInvalidArgumentCount_ReturnsNull()
    {
        var expr = CelExpression.Parse("size()");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_UnknownFunction_ReturnsNull()
    {
        // This tests the default case in CelFunctionCallNode.Evaluate
        var expr = CelExpression.Parse("unknownFunction(\"test\")");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("\"hello\\nworld\"", "hello\nworld")]
    [InlineData("\"tab\\there\"", "tab\there")]
    [InlineData("\"quote\\\"test\"", "quote\"test")]
    [InlineData("\"backslash\\\\test\"", "backslash\\test")]
    [InlineData("'single\\'quote'", "single'quote")]
    [InlineData("\"carriage\\rreturn\"", "carriage\rreturn")]
    public void Parse_StringWithEscapeSequences_ReturnsUnescapedString(string expression, string expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_StringFunction_WithNull_ReturnsEmptyString()
    {
        var expr = CelExpression.Parse("string(null)");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("coalesce(null, null, \"default\")", "default")]
    [InlineData("coalesce()", null)]
    public void Evaluate_CoalesceFunction_WithMultipleNulls_ReturnsFirstNonNull(string expression, object? expected)
    {
        var expr = CelExpression.Parse(expression);
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_UnknownIdentifier_ReturnsNull()
    {
        var expr = CelExpression.Parse("unknownIdentifier");
        var context = new NoCodeExpressionContext(null, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_IndexAccessWithNonIntegerIndex_ReturnsNull()
    {
        var arguments = new object[] { "test" };
        var expr = CelExpression.Parse("arguments[\"notAnInt\"]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_GetPropertyValue_WithException_ReturnsNull()
    {
        var instance = new TestClassWithThrowingProperty();
        var expr = CelExpression.Parse("instance.ThrowingProperty");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_GetIndexValue_WithIList_ReturnsCorrectValue()
    {
        var listArguments = new System.Collections.Generic.List<string> { "first", "second" };
        var arguments = listArguments.ToArray();
        var expr = CelExpression.Parse("arguments[1]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Equal("second", result);
    }

    [Fact]
    public void Evaluate_GetIndexValue_WithNegativeIndex_ReturnsNull()
    {
        var arguments = new object[] { "a", "b", "c" };
        var expr = CelExpression.Parse("arguments[-1]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_GetIndexValue_WithOutOfBoundsIndex_ReturnsNull()
    {
        var arguments = new object[] { "a", "b" };
        var expr = CelExpression.Parse("arguments[10]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_GetIndexValue_WithIListNegativeIndex_ReturnsNull()
    {
        var list = new System.Collections.Generic.List<string> { "a", "b", "c" };
        var arguments = new object[] { list };
        var expr = CelExpression.Parse("arguments[0][-1]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_GetIndexValue_WithIListOutOfBounds_ReturnsNull()
    {
        var list = new System.Collections.Generic.List<string> { "a", "b" };
        var arguments = new object[] { list };
        var expr = CelExpression.Parse("arguments[0][10]");
        var context = new NoCodeExpressionContext(null, arguments, null, null, null);

        var result = expr!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WithExceptionInExpression_ReturnsNull()
    {
        // This should trigger the catch block in CelExpression.Evaluate
        var instance = new TestClassWithThrowingProperty();
        var expr = CelExpression.Parse("instance.ThrowingProperty + \"test\"");
        var context = new NoCodeExpressionContext(instance, null, null, null, null);

        var result = expr!.Evaluate(context);

        // The evaluation should catch the exception and return null
        Assert.Null(result);
    }

    private static NoCodeExpressionContext CreateContext(
            object? instance = null,
            object?[]? arguments = null,
            object? returnValue = null,
            string? methodName = null,
            string? typeName = null)
    {
        return new NoCodeExpressionContext(instance, arguments, returnValue, methodName, typeName);
    }

    private sealed class TestClass
    {
        public string? Name { get; set; }
    }

    private sealed class TestClassWithNested
    {
        public TestClass? Nested { get; set; }
    }

    private sealed class TestClassWithValue
    {
        public int Value { get; set; }
    }

    private sealed class TestClassWithSuccess
    {
        public bool Success { get; set; }
    }

    private sealed class TestClassWithThrowingProperty
    {
#pragma warning disable CA1822 // Narj nenbers as static. Needed for tests.
        public string ThrowingProperty => throw new InvalidOperationException("Property access failed");
#pragma warning restore CA1822 // Narj nenbers as static. Needed for tests.
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode;

public class NoCodeExpressionTests
{
    [Theory]
    [InlineData("$arg1", (int)NoCodeExpressionType.Argument, 1)]
    [InlineData("$arg2", (int)NoCodeExpressionType.Argument, 2)]
    [InlineData("$arg9", (int)NoCodeExpressionType.Argument, 9)]
    [InlineData("$instance", (int)NoCodeExpressionType.Instance, null)]
    [InlineData("$return", (int)NoCodeExpressionType.Return, null)]
    [InlineData("$method", (int)NoCodeExpressionType.MethodName, null)]
    [InlineData("$type", (int)NoCodeExpressionType.TypeName, null)]
    public void Parse_ValidExpressions_ReturnsCorrectType(string expression, int expectedType, int? expectedIndex)
    {
        var result = NoCodeExpression.Parse(expression);

        Assert.NotNull(result);
        Assert.Equal((NoCodeExpressionType)expectedType, result.Type);
        Assert.Equal(expectedIndex, result.ArgumentIndex);
        Assert.Empty(result.PropertyPath);
    }

    [Theory]
    [InlineData("$arg1.Name", new[] { "Name" })]
    [InlineData("$arg1.Customer.Email", new[] { "Customer", "Email" })]
    [InlineData("$instance.ServiceName", new[] { "ServiceName" })]
    [InlineData("$return.Value", new[] { "Value" })]
    public void Parse_PropertyPath_ReturnsCorrectPath(string expression, string[] expectedPath)
    {
        var result = NoCodeExpression.Parse(expression);

        Assert.NotNull(result);
        Assert.Equal(expectedPath, result.PropertyPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("arg1")] // Missing $
    [InlineData("$arg0")] // Invalid index (0)
    [InlineData("$arg10")] // Invalid index (>9)
    [InlineData("$invalid")]
    [InlineData("$arg1[0]")] // Array access not supported in Phase 1
    [InlineData("$arg1.Method()")]  // Method calls not supported in Phase 1
    public void Parse_InvalidExpressions_ReturnsNull(string? expression)
    {
        var result = NoCodeExpression.Parse(expression);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_Argument_ReturnsArgumentValue()
    {
        var expression = NoCodeExpression.Parse("$arg1");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: new object?[] { "test_value" },
            returnValue: null,
            methodName: "TestMethod",
            typeName: "TestType");

        var result = expression!.Evaluate(context);

        Assert.Equal("test_value", result);
    }

    [Fact]
    public void Evaluate_SecondArgument_ReturnsCorrectValue()
    {
        var expression = NoCodeExpression.Parse("$arg2");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: new object?[] { "first", 42, "third" },
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Evaluate_Instance_ReturnsInstanceValue()
    {
        var expression = NoCodeExpression.Parse("$instance");
        var instance = new TestClass("TestInstance");
        var context = new NoCodeExpressionContext(
            instance: instance,
            arguments: null,
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Same(instance, result);
    }

    [Fact]
    public void Evaluate_ArgumentProperty_ReturnsPropertyValue()
    {
        var expression = NoCodeExpression.Parse("$arg1.Name");
        var arg = new TestClass("TestName");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: new object?[] { arg },
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal("TestName", result);
    }

    [Fact]
    public void Evaluate_NestedProperty_ReturnsNestedValue()
    {
        var expression = NoCodeExpression.Parse("$arg1.Nested.Name");
        var arg = new TestClass("Parent", new TestClass("NestedName"));
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [arg],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal("NestedName", result);
    }

    [Fact]
    public void Evaluate_InstanceProperty_ReturnsPropertyValue()
    {
        var expression = NoCodeExpression.Parse("$instance.Name");
        var instance = new TestClass("InstanceName");
        var context = new NoCodeExpressionContext(
            instance: instance,
            arguments: null,
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal("InstanceName", result);
    }

    [Fact]
    public void Evaluate_ReturnValue_ReturnsValue()
    {
        var expression = NoCodeExpression.Parse("$return");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: "return_value",
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal("return_value", result);
    }

    [Fact]
    public void Evaluate_MethodName_ReturnsMethodName()
    {
        var expression = NoCodeExpression.Parse("$method");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: null,
            methodName: "ProcessOrder",
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Equal("ProcessOrder", result);
    }

    [Fact]
    public void Evaluate_TypeName_ReturnsTypeName()
    {
        var expression = NoCodeExpression.Parse("$type");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: null,
            returnValue: null,
            methodName: null,
            typeName: "MyNamespace.OrderService");

        var result = expression!.Evaluate(context);

        Assert.Equal("MyNamespace.OrderService", result);
    }

    [Fact]
    public void Evaluate_NullArgument_ReturnsNull()
    {
        var expression = NoCodeExpression.Parse("$arg1.Name");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: new object?[] { null },
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_NonExistentProperty_ReturnsNull()
    {
        var expression = NoCodeExpression.Parse("$arg1.NonExistentProperty");
        var arg = new TestClass("Test");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: [arg],
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ArgumentOutOfRange_ReturnsNull()
    {
        var expression = NoCodeExpression.Parse("$arg5");
        var context = new NoCodeExpressionContext(
            instance: null,
            arguments: new object?[] { "only_one_arg" },
            returnValue: null,
            methodName: null,
            typeName: null);

        var result = expression!.Evaluate(context);

        Assert.Null(result);
    }

    private sealed class TestClass
    {
        public TestClass(string? name, TestClass? nested = null)
        {
            Name = name;
            Nested = nested;
        }

        public string? Name { get; }

        public TestClass? Nested { get; }
    }
}

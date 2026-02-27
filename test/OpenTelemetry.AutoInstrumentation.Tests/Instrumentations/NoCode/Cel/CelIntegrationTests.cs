// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.NoCode.Cel;

/// <summary>
/// Integration tests for CEL expressions simulating real-world NoCode instrumentation scenarios.
/// </summary>
public class CelIntegrationTests
{
    [Fact]
    public void Scenario_ExtractOrderIdFromArgument()
    {
        var expr = CelExpression.Parse("arguments[0].OrderId");
        var order = new Order { OrderId = "ORD-12345", Amount = 99.99m };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("ORD-12345", result);
    }

    [Fact]
    public void Scenario_BuildSpanNameFromArguments()
    {
        var expr = CelExpression.Parse("concat(\"ProcessOrder.\", arguments[0].OrderId)");
        var order = new Order { OrderId = "ORD-12345" };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("ProcessOrder.ORD-12345", result);
    }

    [Fact]
    public void Scenario_ConditionalStatusFromReturnValue()
    {
        var expr = CelExpression.Parse("return.Success == true");
        var result = new OperationResult { Success = true, Message = "Completed" };
        var context = CreateContext(returnValue: result);

        var evalResult = expr!.Evaluate(context);

        Assert.Equal(true, evalResult);
    }

    [Fact]
    public void Scenario_ExtractNestedCustomerEmail()
    {
        var expr = CelExpression.Parse("arguments[0].Customer.Email");
        var order = new Order
        {
            OrderId = "ORD-12345",
            Customer = new Customer { Name = "John Doe", Email = "john@example.com" }
        };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("john@example.com", result);
    }

    [Fact]
    public void Scenario_CoalesceCustomerNameWithDefault()
    {
        var expr = CelExpression.Parse("coalesce(arguments[0].Customer.Name, \"Anonymous\")");
        var order = new Order { OrderId = "ORD-12345", Customer = null };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("Anonymous", result);
    }

    [Fact]
    public void Scenario_DeterminePriorityBasedOnAmount()
    {
        var expr = CelExpression.Parse("arguments[0].Amount > 100 ? \"high\" : \"normal\"");
        var order = new Order { OrderId = "ORD-12345", Amount = 150.00m };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("high", result);
    }

    [Fact]
    public void Scenario_CheckErrorCondition()
    {
        var expr = CelExpression.Parse("return.Success == false && contains(return.Message, \"error\")");
        var result = new OperationResult { Success = false, Message = "Database error occurred" };
        var context = CreateContext(returnValue: result);

        var evalResult = expr!.Evaluate(context);

        Assert.Equal(true, evalResult);
    }

    [Fact]
    public void Scenario_BuildResourceIdentifier()
    {
        var expr = CelExpression.Parse("concat(\"Order-\", arguments[0].OrderId, \"-\", arguments[1])");
        var order = new Order { OrderId = "12345" };
        var context = CreateContext(arguments: new object?[] { order, "Payment" });

        var result = expr!.Evaluate(context);

        Assert.Equal("Order-12345-Payment", result);
    }

    [Fact]
    public void Scenario_ExtractHttpStatusCode()
    {
        var expr = CelExpression.Parse("return.StatusCode");
        var response = new HttpResponse { StatusCode = 200, Body = "OK" };
        var context = CreateContext(returnValue: response);

        var result = expr!.Evaluate(context);

        Assert.Equal(200, result);
    }

    [Fact]
    public void Scenario_ValidateApiPath()
    {
        var expr = CelExpression.Parse("startsWith(arguments[0].Path, \"/api/v1/\")");
        var request = new ApiRequest { Path = "/api/v1/orders", Method = "GET" };
        var context = CreateContext(arguments: new object?[] { request });

        var result = expr!.Evaluate(context);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Scenario_ExtractMethodAndTypeName()
    {
        var expr = CelExpression.Parse("concat(type, \".\", method)");
        var context = CreateContext(methodName: "ProcessOrder", typeName: "OrderService");

        var result = expr!.Evaluate(context);

        Assert.Equal("OrderService.ProcessOrder", result);
    }

    [Fact]
    public void Scenario_ComplexBusinessRule()
    {
        var expr = CelExpression.Parse(
            "arguments[0].Amount > 1000 && " +
            "arguments[0].Customer != null && " +
            "arguments[0].Customer.IsPremium == true ? \"premium_order\" : \"standard_order\"");
        var order = new Order
        {
            OrderId = "ORD-12345",
            Amount = 1500.00m,
            Customer = new Customer { Name = "John Doe", IsPremium = true }
        };
        var context = CreateContext(arguments: new object?[] { order });

        var result = expr!.Evaluate(context);

        Assert.Equal("premium_order", result);
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

    private sealed class Order
    {
        public string OrderId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public Customer? Customer { get; set; }
    }

    private sealed class Customer
    {
        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public bool IsPremium { get; set; }
    }

    private sealed class OperationResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    private sealed class HttpResponse
    {
        public int StatusCode { get; set; }

        public string Body { get; set; } = string.Empty;
    }

    private sealed class ApiRequest
    {
        public string Path { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;
    }
}

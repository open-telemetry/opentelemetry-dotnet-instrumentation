// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NoCode;

/// <summary>
/// Test class for dynamic attribute extraction from method parameters.
/// </summary>
#pragma warning disable CA1822 // Mark members as static
internal sealed class DynamicAttributeTestingClass
{
    public string ServiceName { get; } = "TestService";

    public int MerchantId { get; } = 12345;

    /// <summary>
    /// Method to test extracting simple argument values.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ProcessOrder(string orderId, int quantity)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test extracting nested property values from arguments.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ProcessCustomer(Customer customer)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test extracting instance properties.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AuditAction(string action)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test concat function with arguments.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CreateResource(string resourceType, string resourceId)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test coalesce function with nullable arguments.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ProcessWithDefault(string? value)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test $method and $type expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OperationWithMetadata()
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Async method to test dynamic attributes with async methods.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<OrderResult> ProcessOrderAsync(Order order)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        return new OrderResult { Success = true, OrderId = order.Id };
    }

    /// <summary>
    /// Method to test $return expression with status rules.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public OrderResult CompleteOrder(string orderId)
    {
        return new OrderResult { Success = true, OrderId = orderId };
    }

    /// <summary>
    /// Method to test dynamic span name with argument value.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ProcessTransaction(string transactionId, string transactionType)
    {
        // This method is intentionally left empty.
    }

    /// <summary>
    /// Method to test dynamic span name with concat function.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ExecuteQuery(string database, string tableName)
    {
        // This method is intentionally left empty.
    }
}
#pragma warning restore CA1822 // Mark members as static


// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

/// <summary>
/// Test class representing an order.
/// </summary>
internal sealed class Order
{
    public string Id { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;
}

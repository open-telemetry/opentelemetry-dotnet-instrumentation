// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

/// <summary>
/// Test class representing an order result.
/// </summary>
internal sealed class OrderResult
{
    public bool Success { get; set; }

    public string OrderId { get; set; } = string.Empty;
}

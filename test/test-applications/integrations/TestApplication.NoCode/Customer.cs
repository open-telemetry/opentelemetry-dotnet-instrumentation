// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

/// <summary>
/// Test class representing a customer with nested properties.
/// </summary>
internal sealed class Customer
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Address? Address { get; set; }
}

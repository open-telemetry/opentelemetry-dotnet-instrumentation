// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

/// <summary>
/// Test class representing an address for nested property access.
/// </summary>
internal sealed class Address
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

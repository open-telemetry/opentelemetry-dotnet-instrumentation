// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.MassTransit.Contracts;

#pragma warning disable CA1515 // Consider making public types internal. It is public because MassTransit needs to access it.
public sealed record TestMessage
#pragma warning disable CA1515 // Consider making public types internal. It is public because MassTransit needs to access it.
{
    public string? Value { get; set; }
}

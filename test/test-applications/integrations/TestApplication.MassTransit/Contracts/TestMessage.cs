// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.MassTransit.Contracts;

public record TestMessage
{
    public string? Value { get; set; }
}

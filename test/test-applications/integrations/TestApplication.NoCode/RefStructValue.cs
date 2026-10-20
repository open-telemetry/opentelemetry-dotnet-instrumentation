// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

internal readonly ref struct RefStructValue(int value)
{
    public int Value { get; } = value;
}

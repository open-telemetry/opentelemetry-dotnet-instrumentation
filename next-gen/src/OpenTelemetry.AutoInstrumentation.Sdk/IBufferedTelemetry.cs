// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

internal interface IBufferedTelemetry<T>
    where T : class
{
    InstrumentationScope Scope { get; }

    T? Next { get; set; }
}

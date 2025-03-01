// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.OpenTelemetryProtocol;

internal interface IOtlpBatchWriter<TRequest>
{
    TRequest Request { get; }

    void Reset();
}

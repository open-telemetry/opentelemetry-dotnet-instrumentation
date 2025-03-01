// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Describes the contract for exporting batches of spans asynchronously.
/// </summary>
public interface ISpanExporterAsync : IExporterAsync<SpanBatchWriter>
{
}

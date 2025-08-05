// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.Tracing;

/// <summary>
/// Describes a <see cref="IBatchWriter"/> for writing batches of spans.
/// </summary>
public abstract class SpanBatchWriter : IBatchWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpanBatchWriter"/> class.
    /// </summary>
    protected SpanBatchWriter()
    {
    }

    /// <inheritdoc/>
    public virtual void BeginBatch(
        Resource resource)
    {
    }

    /// <inheritdoc/>
    public virtual void EndBatch()
    {
    }

    /// <inheritdoc/>
    public virtual void BeginInstrumentationScope(
        InstrumentationScope instrumentationScope)
    {
    }

    /// <inheritdoc/>
    public virtual void EndInstrumentationScope()
    {
    }

    /// <summary>
    /// Write a span.
    /// </summary>
    /// <param name="span"><see cref="Span"/>.</param>
    public virtual void WriteSpan(in Span span)
    {
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Describes the contract for a span processor.
/// </summary>
public interface ISpanProcessor : IProcessor
{
    /// <summary>
    /// Process an ended span.
    /// </summary>
    /// <param name="span"><see cref="Span"/>.</param>
    void ProcessEndedSpan(in Span span);
}

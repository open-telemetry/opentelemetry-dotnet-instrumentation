// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection;

/// <summary>
/// Duck-typed interface for accessing NLog's LogEventInfo.Properties.
/// This interface is used for trace context injection to add TraceId, SpanId,
/// and TraceFlags to the log event properties.
/// </summary>
/// <remarks>
/// NLog's LogEventInfo.Properties is of type IDictionary{object, object}.
/// We use the generic interface to match NLog's property type.
/// </remarks>
internal interface ILogEventInfoProperties
{
    /// <summary>
    /// Gets the properties dictionary for the log event.
    /// </summary>
    public IDictionary<object, object>? Properties { get; }
}

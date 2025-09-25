// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection;

/// <summary>
/// Constants used for injecting trace context information into NLog log events.
/// These constants define the property names used to store OpenTelemetry trace data
/// within NLog's properties collection.
/// </summary>
internal static class LogsTraceContextInjectionConstants
{
    /// <summary>
    /// Property name for storing the OpenTelemetry span ID in NLog properties.
    /// </summary>
    public const string SpanIdPropertyName = "SpanId";

    /// <summary>
    /// Property name for storing the OpenTelemetry trace ID in NLog properties.
    /// </summary>
    public const string TraceIdPropertyName = "TraceId";

    /// <summary>
    /// Property name for storing the OpenTelemetry trace flags in NLog properties.
    /// </summary>
    public const string TraceFlagsPropertyName = "TraceFlags";
}

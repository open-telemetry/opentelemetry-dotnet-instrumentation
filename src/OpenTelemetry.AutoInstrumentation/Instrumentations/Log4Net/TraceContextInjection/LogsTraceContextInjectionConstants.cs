// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.TraceContextInjection;

internal static class LogsTraceContextInjectionConstants
{
    public const string SpanIdPropertyName = "span_id";
    public const string TraceIdPropertyName = "trace_id";
    public const string TraceFlagsPropertyName = "trace_flags";
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

internal static class WcfClientInstrumentation
{
    public static bool ValidateExpectedSpanHierarchy(ICollection<MockSpansCollector.Collected> assertedSpans)
    {
        var customParent = assertedSpans.Single(collected =>
            collected.InstrumentationScopeName.StartsWith("TestApplication.Wcf.Client", StringComparison.Ordinal) &&
            collected.Span.Name == "Parent");
        var customSibling = assertedSpans.Single(collected =>
            collected.InstrumentationScopeName.StartsWith("TestApplication.Wcf.Client", StringComparison.Ordinal) &&
            collected.Span.Name == "Sibling");
        var wcfClientSpans = assertedSpans.Where(collected =>
            collected.Span.Kind == Span.Types.SpanKind.Client &&
            collected.InstrumentationScopeName == "OpenTelemetry.Instrumentation.Wcf");

        return wcfClientSpans.All(span => span.Span.ParentSpanId == customParent.Span.SpanId) &&
               customSibling.Span.ParentSpanId == customParent.Span.SpanId;
    }
}

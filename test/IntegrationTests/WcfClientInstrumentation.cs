// <copyright file="WcfClientInstrumentation.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

internal static class WcfClientInstrumentation
{
    public static bool ValidateExpectedSpanHierarchy(ICollection<MockSpansCollector.Collected> assertedSpans)
    {
        var customParent = assertedSpans.Single(collected =>
            collected.InstrumentationScopeName.StartsWith("TestApplication.Wcf.Client") &&
            collected.Span.Name == "Parent");
        var customSibling = assertedSpans.Single(collected =>
            collected.InstrumentationScopeName.StartsWith("TestApplication.Wcf.Client") &&
            collected.Span.Name == "Sibling");
        var wcfClientSpans = assertedSpans.Where(collected =>
            collected.Span.Kind == Span.Types.SpanKind.Client &&
            collected.InstrumentationScopeName == "OpenTelemetry.Instrumentation.Wcf");

        return wcfClientSpans.All(span => span.Span.ParentSpanId == customParent.Span.SpanId) &&
               customSibling.Span.ParentSpanId == customParent.Span.SpanId;
    }
}

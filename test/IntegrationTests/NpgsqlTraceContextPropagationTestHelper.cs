// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

internal static class NpgsqlTraceContextPropagationTestHelper
{
    public const string ScopeName = "Npgsql";
    public const string ContextPropagationEnvVar = "OTEL_DOTNET_AUTO_NPGSQL_CONTEXT_PROPAGATION";

    private const string ApplicationNameOutputPrefix = "ApplicationName=";

    public static IReadOnlyList<string> ExtractApplicationNames(string standardOutput, int expectedCount)
    {
        var applicationNames = ExtractValues(standardOutput, ApplicationNameOutputPrefix);

        Assert.Equal(expectedCount, applicationNames.Count);
        AssertAllTraceParents(applicationNames);

        return applicationNames;
    }

    public static IReadOnlyList<string> ExtractValues(string standardOutput, string outputPrefix)
    {
        return standardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(outputPrefix, StringComparison.Ordinal))
            .Select(line => line.Substring(outputPrefix.Length))
            .ToList();
    }

    public static void AssertAllTraceParents(IEnumerable<string> applicationNames)
    {
        Assert.All(applicationNames, applicationName => Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$", applicationName));
    }

    public static bool SupportsCopyTracing(string packageVersion)
    {
        if (!string.IsNullOrEmpty(packageVersion))
        {
            return Version.Parse(packageVersion).Major >= 10;
        }

#if NETFRAMEWORK
        return false;
#else
        return true;
#endif
    }

    public static bool MatchesApplicationName(Span span, string applicationName)
    {
        var traceId = ActivityTraceId.CreateFromBytes(span.TraceId.ToByteArray());
        var spanId = ActivitySpanId.CreateFromBytes(span.SpanId.ToByteArray());

        return string.Equals(applicationName, $"00-{traceId}-{spanId}-{span.Flags & byte.MaxValue:x2}", StringComparison.Ordinal);
    }
}

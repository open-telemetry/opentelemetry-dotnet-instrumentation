// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

internal static class SqlClientTraceContextPropagationTestHelper
{
    public const string ScopeName = "OpenTelemetry.Instrumentation.SqlClient";
    public const string ContextPropagationEnvVar = "OTEL_DOTNET_EXPERIMENTAL_SQLCLIENT_ENABLE_TRACE_CONTEXT_PROPAGATION";

    private const string GetContextInfoQuery = "SELECT CONTEXT_INFO()";
    private const string CommandResultOutputPrefix = "CommandResult=";

    public static string GetContextInfoCommandArguments(bool enableTransaction)
    {
        return enableTransaction
            ? $"--command-text \"{GetContextInfoQuery}\" --enable-transaction"
            : $"--command-text \"{GetContextInfoQuery}\"";
    }

    public static string ExtractContextInfo(string standardOutput)
    {
        var line = standardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .SingleOrDefault(line => line.StartsWith(CommandResultOutputPrefix, StringComparison.Ordinal));

        Assert.False(line is null, $"Test application did not print {CommandResultOutputPrefix}.");

        var contextInfo = line!.Substring(CommandResultOutputPrefix.Length);
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$", contextInfo);

        return contextInfo;
    }

    public static bool MatchesContextInfo(Span span, string contextInfo)
    {
        var traceId = ActivityTraceId.CreateFromBytes(span.TraceId.ToByteArray());
        var spanId = ActivitySpanId.CreateFromBytes(span.SpanId.ToByteArray());

        return contextInfo.StartsWith($"00-{traceId}-{spanId}-", StringComparison.Ordinal);
    }
}

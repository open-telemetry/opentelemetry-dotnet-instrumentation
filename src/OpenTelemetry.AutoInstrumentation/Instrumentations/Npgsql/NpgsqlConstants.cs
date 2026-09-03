// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql;

internal static class NpgsqlConstants
{
    public const string IntegrationName = nameof(TracerInstrumentation.Npgsql);
    public const string AssemblyName = "Npgsql";
    public const string CommandTypeName = "Npgsql.NpgsqlCommand";
    public const string ConnectorTypeName = "Npgsql.Internal.NpgsqlConnector";
    public const string TraceCommandStartMethodName = "TraceCommandStart";
    public const string TraceCommandEnrichMethodName = "TraceCommandEnrich";
    public const string EndUserActionMethodName = "EndUserAction";
    public const string MinVersion = "6.0.0";
    public const string TraceCommandStart6MaxVersion = "6.0.11";
    public const string TraceCommandEnrich6MinVersion = "6.0.12";
    public const string TraceCommandEnrich6MaxVersion = "6.*.*";
    public const string TraceCommandStart7MinVersion = "7.0.0";
    public const string TraceCommandStart7MaxVersion = "7.0.7";
    public const string TraceCommandEnrich7MinVersion = "7.0.8";
    public const string TraceCommandEnrich7MaxVersion = "7.*.*";
    public const string TraceCommandStart8MinVersion = "8.0.0";
    public const string TraceCommandStart8MaxVersion = "8.0.3";
    public const string TraceCommandEnrich8MinVersion = "8.0.4";
#if NETFRAMEWORK
    // Npgsql 9 and later do not support .NET Framework.
    public const string MaxVersion = "8.*.*";
#else
    public const string TraceCopyStartMethodName = "TraceCopyStart";
    public const string ActivityTypeName = "System.Diagnostics.Activity";
    public const string TraceCopyStartMinVersion = "10.0.0";
    public const string MaxVersion = "10.*.*";
#endif
}

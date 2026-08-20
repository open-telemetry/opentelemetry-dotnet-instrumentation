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
    public const string TraceCommandStartMaxVersion = "6.*.*";
    public const string TraceCommandEnrichMinVersion = "7.0.0";
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

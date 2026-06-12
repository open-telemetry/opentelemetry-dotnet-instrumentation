// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda;

internal static class OracleMdaConstants
{
    public const string IntegrationName = nameof(TracerInstrumentation.OracleMda);
    public const string OracleMdaAssemblyName = "Oracle.ManagedDataAccess";
    public const string OracleConnectionTypeName = "Oracle.ManagedDataAccess.Client.OracleConnection";
    public const string MinVersion = "23.1.0";
    public const string MaxVersion = "23.65535.65535";
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda;

internal static class OracleMdaConstants
{
    public const string IntegrationName = nameof(TracerInstrumentation.OracleMda);
    public const string OracleMdaAssemblyName = "Oracle.ManagedDataAccess";
    public const string OracleConnectionTypeName = "Oracle.ManagedDataAccess.Client.OracleConnection";
    public const string MaxVersion = "23.65535.65535";

#if NETFRAMEWORK
    // Oracle.ManagedDataAccess 23.x NuGet packages for .NET Framework keep the legacy 4.122.23.x assembly version.
    // Integration version matching uses assembly major/minor/patch only, so this matches assembly version 4.122.23.1.
    public const string MinVersion = "4.122.23";
#else
    // Oracle.ManagedDataAccess.Core 23.x NuGet packages load Oracle.ManagedDataAccess with assembly version 23.1.0.0.
    public const string MinVersion = "23.1.0";
#endif
}

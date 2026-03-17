// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetConstants
{
    public const string IntegrationName = "AdoNet";
    public const string DbCommandTypeName = "System.Data.Common.DbCommand";

    // Assembly names
    public const string SystemDataCommonAssemblyName = "System.Data.Common";
    public const string SystemDataAssemblyName = "System.Data";
    public const string NetStandardAssemblyName = "netstandard";

    // Version ranges for System.Data.Common (.NET)
    public const string SystemDataCommonMinVersion = "4.0.0";
    public const string SystemDataCommonMaxVersion = "10.*.*";

    // Version ranges for System.Data (.NET Framework)
    public const string SystemDataMinVersion = "2.0.0";
    public const string SystemDataMaxVersion = "4.*.*";

    // Async methods on System.Data start from v4.0.0
    public const string SystemDataAsyncMinVersion = "4.0.0";

    // Version ranges for netstandard
    public const string NetStandardMinVersion = "2.0.0";
    public const string NetStandardMaxVersion = "2.*.*";

    // Method names
    public const string ExecuteNonQueryMethodName = "ExecuteNonQuery";
    public const string ExecuteNonQueryAsyncMethodName = "ExecuteNonQueryAsync";
    public const string ExecuteScalarMethodName = "ExecuteScalar";
    public const string ExecuteScalarAsyncMethodName = "ExecuteScalarAsync";
    public const string ExecuteDbDataReaderMethodName = "ExecuteDbDataReader";
    public const string ExecuteDbDataReaderAsyncMethodName = "ExecuteDbDataReaderAsync";

    // Type names for return values and parameters
    public const string CommandBehaviorTypeName = "System.Data.CommandBehavior";
    public const string DbDataReaderTypeName = "System.Data.Common.DbDataReader";
    public const string DbDataReaderTaskTypeName = "System.Threading.Tasks.Task`1[System.Data.Common.DbDataReader]";
}

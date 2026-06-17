// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.SqlClient;

internal static class SqlClientConstants
{
    public const string IntegrationName = "SqlClient";

    public const string SystemDataAssemblyName = "System.Data";
    public const string SystemDataSqlClientAssemblyName = "System.Data.SqlClient";
    public const string MicrosoftDataSqlClientAssemblyName = "Microsoft.Data.SqlClient";

    public const string SystemDataSqlCommandTypeName = "System.Data.SqlClient.SqlCommand";
    public const string MicrosoftDataSqlCommandTypeName = "Microsoft.Data.SqlClient.SqlCommand";

    public const string SystemDataMinVersion = "2.0.0";
    public const string SystemDataMaxVersion = "4.*.*";
    public const string SystemDataSqlClientMinVersion = "4.0.0";
    public const string SystemDataSqlClientMaxVersion = "5.*.*";
    public const string MicrosoftDataSqlClientMinVersion = "1.0.0";
    public const string MicrosoftDataSqlClientMaxVersion = "7.*.*";

    public const string WriteBeginExecuteEventMethodName = "WriteBeginExecuteEvent";
}
#endif

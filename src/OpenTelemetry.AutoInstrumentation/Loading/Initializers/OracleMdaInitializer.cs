// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class OracleMdaInitializer : InstrumentationInitializer
{
    private readonly TracerSettings _tracerSettings;

    public OracleMdaInitializer(TracerSettings tracerSettings)
        : base("Oracle.ManagedDataAccess", nameof(OracleMdaInitializer))
    {
        _tracerSettings = tracerSettings;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var oracleCommandType = Type.GetType("Oracle.ManagedDataAccess.Client.OracleCommand, Oracle.ManagedDataAccess");
        var oracleActivitySourceType = Type.GetType("Oracle.ManagedDataAccess.OpenTelemetry.OracleActivitySource, Oracle.ManagedDataAccess");

        if (oracleCommandType == null || oracleActivitySourceType == null)
        {
            return;
        }

        var oracleActivitySourceFieldInfo = oracleCommandType.GetField("OracleActivitySource", BindingFlags.Static | BindingFlags.NonPublic);
        var oracleActivitySourceField = oracleActivitySourceFieldInfo?.GetValue(null);

        if (oracleActivitySourceField == null)
        {
            return;
        }

        var setDbStatementForTextPropertyInfo = oracleActivitySourceType.GetProperty("SetDbStatementForText", BindingFlags.Instance | BindingFlags.NonPublic);

        setDbStatementForTextPropertyInfo?.SetValue(oracleActivitySourceField, _tracerSettings.InstrumentationOptions.OracleMdaSetDbStatementForText);
    }
}

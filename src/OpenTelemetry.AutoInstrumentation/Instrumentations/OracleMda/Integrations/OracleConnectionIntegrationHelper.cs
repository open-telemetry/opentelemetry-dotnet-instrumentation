// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.Integrations;

internal static class OracleConnectionIntegrationHelper
{
    public static void SetDatabaseOpenTelemetryTracing(object instance)
    {
        if (Instrumentation.TracerSettings.Value.InstrumentationOptions.OracleMdaDatabaseOpenTelemetryTracing &&
            instance.TryDuckCast<IOracleConnection>(out var oracleConnection))
        {
            oracleConnection.DatabaseOpenTelemetryTracing = true;
        }
    }
}

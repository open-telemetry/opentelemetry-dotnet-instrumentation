// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.DuckTypes;

internal interface IOracleConnection : IDuckType
{
    bool DatabaseOpenTelemetryTracing { get; set; }
}

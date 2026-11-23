// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql.DuckTypes;

internal interface INpgsqlConnector : IDuckType
{
    object Settings { get; }

    byte TransactionStatus { get; }

    void ExecuteInternalCommand(string query);
}

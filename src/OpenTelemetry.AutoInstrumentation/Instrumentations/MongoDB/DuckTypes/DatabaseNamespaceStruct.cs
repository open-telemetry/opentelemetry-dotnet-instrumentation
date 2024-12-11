// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.DatabaseNamespace interface for duck-typing
/// </summary>
[DuckCopy]
internal struct DatabaseNamespaceStruct
{
    /// <summary>
    /// Gets the name of the database
    /// </summary>
    public string? DatabaseName;
}

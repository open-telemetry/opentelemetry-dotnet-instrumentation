// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Bson.BsonDocument interface for duck-typing
/// </summary>
[DuckCopy]
internal struct BsonElementStruct
{
    /// <summary>
    /// Gets the name of the element.
    /// </summary>
    public string Name;

    /// <summary>
    /// Gets the value of the element.
    /// </summary>
    public object? Value;
}

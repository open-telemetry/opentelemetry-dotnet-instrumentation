// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Bson.BsonArray interface for duck-typing
/// </summary>
internal interface IBsonArrayProxy : IDuckType
{
    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count { get; }
}

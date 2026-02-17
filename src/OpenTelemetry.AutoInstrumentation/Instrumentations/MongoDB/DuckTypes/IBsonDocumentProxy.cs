// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Bson.BsonDocument interface for duck-typing
/// </summary>
internal interface IBsonDocumentProxy : IDuckType
{
    /// <summary>
    /// Gets the number of elements.
    /// </summary>
    public int ElementCount { get; }

    /// <summary>
    /// Gets an element of this document.
    /// </summary>
    /// <param name="index">The zero based index of the element.</param>
    /// <returns>The element.</returns>
    public BsonElementStruct GetElement(int index);
}

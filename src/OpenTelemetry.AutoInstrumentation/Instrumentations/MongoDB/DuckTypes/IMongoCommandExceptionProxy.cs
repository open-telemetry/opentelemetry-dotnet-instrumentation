// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.MongoCommandException interface for duck-typing
/// </summary>
internal interface IMongoCommandExceptionProxy : IDuckType
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public int Code { get; }
}

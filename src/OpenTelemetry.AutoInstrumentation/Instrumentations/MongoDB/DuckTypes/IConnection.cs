// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.Core.IConnection interface for duck-typing
/// </summary>
internal interface IConnection : IDuckType
{
    /// <summary>
    /// Gets the command object passed into the wire protocol
    /// </summary>
    EndPoint? EndPoint { get; }
}

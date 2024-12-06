// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.Core.IWireProtocol interface for duck-typing
/// </summary>
[DuckCopy]
internal struct IWireProtocolWithCommandStruct
{
    /// <summary>
    /// Gets the command object passed into the wire protocol
    /// </summary>
    [DuckField(Name = "_command")]
    public object? Command;
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

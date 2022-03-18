// <copyright file="IWireProtocol_Execute_Integration.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.ComponentModel;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDb;

/// <summary>
/// MongoDB.Driver.Core.WireProtocol.IWireProtocol instrumentation
/// </summary>
[MongoDbExecute(
    typeName: "MongoDB.Driver.Core.WireProtocol.KillCursorsWireProtocol",
    isGeneric: false)]
// ReSharper disable once InconsistentNaming
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class IWireProtocol_Execute_Integration
{
    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="connection">The MongoDB connection</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <returns>Calltarget state value</returns>
    public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, object connection, CancellationToken cancellationToken)
    {
        var activity = MongoDbIntegration.CreateActivity(instance, connection);

        return new CallTargetState(activity);
    }

    /// <summary>
    /// OnAsyncMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
    {
        state.Activity.DisposeWithException(exception);

        return CallTargetReturn.GetDefault();
    }
}

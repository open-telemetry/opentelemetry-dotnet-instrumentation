using System;
using System.Threading;
using OpenTelemetry.ClrProfiler.CallTarget;
using OpenTelemetry.ClrProfiler.Managed.Util;

namespace OpenTelemetry.ClrProfiler.AutoInstrumentation.MongoDb
{
    /// <summary>
    /// MongoDB.Driver.Core.WireProtocol.IWireProtocol instrumentation
    /// </summary>
    [MongoDbExecute(
        typeName: "MongoDB.Driver.Core.WireProtocol.KillCursorsWireProtocol",
        isGeneric: false)]
    // ReSharper disable once InconsistentNaming
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
}

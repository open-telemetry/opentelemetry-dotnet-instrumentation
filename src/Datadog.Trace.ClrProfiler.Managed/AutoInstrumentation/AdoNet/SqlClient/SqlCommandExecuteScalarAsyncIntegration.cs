using System;
using System.Data.Common;
using System.Threading;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AdoNet.SqlClient
{
    /// <summary>
    /// CallTarget instrumentation for:
    /// Task[object] System.Data.SqlClient.SqlCommand.ExecuteScalarAsync(CancellationToken)
    /// Task[object] Microsoft.Data.SqlClient.SqlCommand.ExecuteScalarAsync(CancellationToken)
    /// </summary>
    [SqlClientConstants.SystemData.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteScalarAsync,
        ReturnTypeName = "System.Threading.Tasks.Task`1<System.Object>",
        ParametersTypesNames = new[] { ClrNames.CancellationToken })]
    [SqlClientConstants.MicrosoftDataSqlClient.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteScalarAsync,
        ReturnTypeName = "System.Threading.Tasks.Task`1<System.Object>",
        ParametersTypesNames = new[] { ClrNames.CancellationToken })]
    public class SqlCommandExecuteScalarAsyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CancellationToken cancellationToken)
        {
            return new CallTargetState(ScopeFactory.CreateDbCommandScope(Tracer.Instance, instance as DbCommand));
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value, in an async scenario will be T of Task of T</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Return value instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        public static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            state.Scope.DisposeWithException(exception);
            return returnValue;
        }
    }
}

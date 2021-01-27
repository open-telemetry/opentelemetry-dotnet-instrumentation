using System;
using System.Data.Common;
using System.Threading;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AdoNet.SqlClient
{
    /// <summary>
    /// CallTarget instrumentation for:
    /// Task[SqlDataReader] System.Data.SqlClient.SqlCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)
    /// Task[DbDataReader] System.Data.SqlClient.SqlCommand.ExecuteDbDataReaderAsync(CommandBehavior, CancellationToken)
    /// Task[SqlDataReader] Microsoft.Data.SqlClient.SqlCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)
    /// Task[DbDataReader] Microsoft.Data.SqlClient.SqlCommand.ExecuteDbDataReaderAsync(CommandBehavior, CancellationToken)
    /// </summary>
    [SqlClientConstants.SystemData.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteReaderAsync,
        ReturnTypeName = SqlClientConstants.SystemData.SqlDataReaderTaskType,
        ParametersTypesNames = new[] { AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken })]
    [SqlClientConstants.SystemData.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteDbDataReaderAsync,
        ReturnTypeName = AdoNetConstants.TypeNames.DbDataReaderTaskType,
        ParametersTypesNames = new[] { AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken })]
    [SqlClientConstants.MicrosoftDataSqlClient.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteReaderAsync,
        ReturnTypeName = SqlClientConstants.MicrosoftDataSqlClient.SqlDataReaderTaskType,
        ParametersTypesNames = new[] { AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken })]
    [SqlClientConstants.MicrosoftDataSqlClient.InstrumentSqlCommand(
        Method = AdoNetConstants.MethodNames.ExecuteDbDataReaderAsync,
        ReturnTypeName = AdoNetConstants.TypeNames.DbDataReaderTaskType,
        ParametersTypesNames = new[] { AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken })]
    public class SqlCommandExecuteReaderAsyncIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TBehavior">Command Behavior type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="commandBehavior">Command behavior</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TBehavior>(TTarget instance, TBehavior commandBehavior, CancellationToken cancellationToken)
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

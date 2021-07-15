namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Instrumentations.GraphQL
{
    internal class GraphQLExecuteAsyncAttribute : InstrumentMethodAttribute
    {
        public GraphQLExecuteAsyncAttribute()
        {
            IntegrationName = GraphQLCommon.IntegrationName;
            MethodName = "ExecuteAsync";
            ReturnTypeName = "System.Threading.Tasks.Task`1<GraphQL.ExecutionResult>";
            ParameterTypeNames = new[] { "GraphQL.Execution.ExecutionContext" };
        }
    }
}

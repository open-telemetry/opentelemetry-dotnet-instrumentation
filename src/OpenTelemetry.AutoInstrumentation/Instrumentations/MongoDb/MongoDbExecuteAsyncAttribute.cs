namespace OpenTelemetry.ClrProfiler.Managed.Instrumentations.MongoDb
{
    internal class MongoDbExecuteAsyncAttribute : MongoDbInstrumentMethodAttribute
    {
        internal const string Major2 = "2";
        internal const string Major2Minor1 = "2.1";

        public MongoDbExecuteAsyncAttribute(string typeName, bool isGeneric)
            : base(typeName)
        {
            MinimumVersion = Major2Minor1;
            MaximumVersion = Major2;
            MethodName = "ExecuteAsync";
            ParameterTypeNames = new[] { "MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken };

            if (isGeneric)
            {
                ReturnTypeName = "System.Threading.Tasks.Task`1<T>";
            }
            else
            {
                ReturnTypeName = "System.Threading.Tasks.Task";
            }
        }
    }
}

namespace OpenTelemetry.ClrProfiler.AutoInstrumentation.MongoDb
{
    internal class MongoDbExecuteAttribute : MongoDbInstrumentMethodAttribute
    {
        internal const string Major2 = "2";
        internal const string Major2Minor2 = "2.2"; // Synchronous methods added in 2.2

        public MongoDbExecuteAttribute(string typeName, bool isGeneric)
            : base(typeName)
        {
            MinimumVersion = Major2Minor2;
            MaximumVersion = Major2;
            MethodName = "Execute";
            ParameterTypeNames = new[] { "MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken };

            if (isGeneric)
            {
                ReturnTypeName = "T";
            }
            else
            {
                ReturnTypeName = ClrNames.Void;
            }
        }
    }
}

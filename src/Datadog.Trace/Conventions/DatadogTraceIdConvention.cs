namespace Datadog.Trace.Conventions
{
    internal class DatadogTraceIdConvention : ITraceIdConvention
    {
        public TraceId GenerateNewTraceId() => TraceId.CreateRandomDataDogCompatible();

        public TraceId CreateFromString(string id) => TraceId.CreateDataDogCompatibleFromDecimalString(id);
    }
}

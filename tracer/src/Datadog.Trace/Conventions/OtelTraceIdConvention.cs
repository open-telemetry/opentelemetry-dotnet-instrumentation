namespace Datadog.Trace.Conventions
{
    internal class OtelTraceIdConvention : ITraceIdConvention
    {
        public TraceId GenerateNewTraceId() => TraceId.CreateRandom();

        public TraceId CreateFromString(string id) => TraceId.CreateFromString(id);
    }
}

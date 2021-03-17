namespace Datadog.Trace.Conventions
{
    internal class OtelTraceIdConvention : ITraceIdConvention
    {
        public TraceId GenerateNewTraceId() => TraceId.CreateRandom();
    }
}

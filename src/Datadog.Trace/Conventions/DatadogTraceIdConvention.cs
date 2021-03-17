namespace Datadog.Trace.Conventions
{
    internal class DatadogTraceIdConvention : ITraceIdConvention
    {
        public TraceId GenerateNewTraceId() => TraceId.CreateRandom64Bit();
    }
}

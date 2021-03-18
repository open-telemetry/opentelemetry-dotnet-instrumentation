namespace Datadog.Trace.Conventions
{
    /// <summary>
    /// Convention used when defining format of TraceId.
    /// </summary>
    internal interface ITraceIdConvention
    {
        TraceId GenerateNewTraceId();

        TraceId CreateFromString(string id);
    }
}

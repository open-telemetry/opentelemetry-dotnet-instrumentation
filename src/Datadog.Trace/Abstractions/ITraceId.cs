namespace Datadog.Trace.Abstractions
{
    internal interface ITraceId
    {
        ulong Lower { get; }
    }
}

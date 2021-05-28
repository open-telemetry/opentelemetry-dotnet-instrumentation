namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace.Abstractions
{
    /// <summary>
    /// Interface for scope getter and setter access
    /// </summary>
    internal interface IScopeRawAccess
    {
        Scope Active { get; set; }
    }
}

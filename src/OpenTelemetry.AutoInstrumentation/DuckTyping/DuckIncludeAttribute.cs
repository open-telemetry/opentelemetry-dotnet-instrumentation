using System;

namespace OpenTelemetry.ClrProfiler.DuckTyping
{
    /// <summary>
    /// Use to include a member that would normally be ignored
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DuckIncludeAttribute : Attribute
    {
    }
}

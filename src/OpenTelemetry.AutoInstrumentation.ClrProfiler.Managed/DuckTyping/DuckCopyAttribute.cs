using System;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.DuckTyping
{
    /// <summary>
    /// Duck copy struct attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class DuckCopyAttribute : Attribute
    {
    }
}

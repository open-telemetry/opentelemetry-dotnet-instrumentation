using System;

namespace OpenTelemetry.AutoInstrumentation.CallTarget
{
    /// <summary>
    /// Apply on a calltarget async callback to indicate that the method
    /// should execute under the current synchronization context/task scheduler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class PreserveContextAttribute : Attribute
    {
    }
}

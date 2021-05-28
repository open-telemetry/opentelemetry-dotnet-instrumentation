using System;
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace.Abstractions
{
    internal interface IScope : IDisposable
    {
        Activity Activity { get; }
    }
}

using System;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util
{
    internal static class ScopeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeWithException(this Scope scope, Exception exception)
        {
            if (scope != null)
            {
                try
                {
                    if (exception != null)
                    {
                        scope.Activity?.SetException(exception);
                    }
                }
                finally
                {
                    scope.Dispose();
                }
            }
        }
    }
}

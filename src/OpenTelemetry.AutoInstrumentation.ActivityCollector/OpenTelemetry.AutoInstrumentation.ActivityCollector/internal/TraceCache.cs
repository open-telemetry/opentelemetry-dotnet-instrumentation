using System;
using System.Collections.Concurrent;

namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    /// <summary>
    /// For now, this is a thin wrapper around a dictionary.
    /// 
    /// There are several strategies how grouping spans to local traces may be made significantly faster:
    /// 
    ///  * We may use the a custom lookup optimized based on reasonable assumptions about the number of concurrent
    ///    requests (aka Traces), we can optimize the lookups here.
    /// 
    /// * (probably most promising as of now) Move the class LocalTrace into OpenTelemetry.DynamicActivityBinding and 
    ///   store the trace spans list in the local trace root as follows: 
    ///   - Dynamically emit and cache a type using reflection emit when the Activity type has been loaded.
    ///     The type fill add a single field that stores a reference to strongly-typed (i.e. direct lookup)
    ///     property-bag with relevant activity info:
    ///     
    ///     namespace OpenTelemetry.DynamicActivityBinding
    ///     {
    ///         internal class AutocollectedActivity : Activity
    ///         {
    ///             public SupplementalActivityData TracerData;
    ///         }
    ///         
    ///         internal class SupplementalActivityData
    ///         {
    ///             public LocalTrace Trace { get; }
    ///             public Xxx OtherTracerInfo { get; }
    ///         }
    ///     }
    ///     
    ///     All Activites created by the auto-instrumentation (i.e. using stubs) will be of runtyme type AutocollectedActivity.
    ///     The lookup of the local trace info can happen can happen by walking up the parent chan until the local root is
    ///     found (parent is null), and then by checking checking for the runtime type of the root activity object; 
    ///     if it is a AutocollectedActivity, just dereference ((AutocollectedActivity) actityInstance).ActivityTracerData.Trace;
    ///     if it is not, use a ConditionalWeakTable like already prototyped.
    /// </summary>
    internal class TraceCache
    {
        private readonly ConcurrentDictionary<ulong, LocalTrace> _traces = new ConcurrentDictionary<ulong, LocalTrace>();

        public bool TryAddNew(ulong traceKey, LocalTrace trace)
        {
            return _traces.TryAdd(traceKey, trace);
        }

        public bool TryGet(ulong traceKey, out LocalTrace trace)
        {
            return _traces.TryGetValue(traceKey, out trace);
        }

        public bool TryRemove(ulong traceKey, out LocalTrace trace)
        {
            return _traces.TryRemove(traceKey, out trace);
        }
    }
}

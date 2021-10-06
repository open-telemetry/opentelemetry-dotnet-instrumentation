using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// IRegionEndpoint interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionEndpoint
    {
        /// <summary>
        /// Gets the system name of the region endpoint
        /// </summary>
        string SystemName { get; }
    }
}

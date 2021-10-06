using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis.StackExchange
{
    /// <summary>
    /// Connection multiplexer ducktype structure
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IConnectionMultiplexer
    {
        /// <summary>
        /// Gets the conection configuration
        /// </summary>
        string Configuration { get; }
    }
}

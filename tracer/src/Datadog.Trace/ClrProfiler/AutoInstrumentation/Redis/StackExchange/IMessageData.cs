using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis.StackExchange
{
    /// <summary>
    /// Message data interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMessageData
    {
        /// <summary>
        /// Gets message command and key
        /// </summary>
        public string CommandAndKey { get; }
    }
}

using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis.StackExchange
{
    /// <summary>
    /// RedisBase interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRedisBase
    {
        /// <summary>
        /// Gets multiplexer data structure
        /// </summary>
        [DuckField(Name = "multiplexer")]
        public MultiplexerData Multiplexer { get; }
    }
}

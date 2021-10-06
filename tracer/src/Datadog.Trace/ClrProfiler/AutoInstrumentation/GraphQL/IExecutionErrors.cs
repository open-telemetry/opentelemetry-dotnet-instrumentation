using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.GraphQL
{
    /// <summary>
    /// GraphQL.ExecutionErrors interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IExecutionErrors
    {
        /// <summary>
        /// Gets the number of errors
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the ExecutionError at the specified index
        /// </summary>
        /// <param name="index">Index to lookup</param>
        /// <returns>An execution error</returns>
        IExecutionError this[int index] { get; }
    }
}

using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.GraphQL
{
    /// <summary>
    /// GraphQL.Validation.IValidationResult interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IValidationResult
    {
        /// <summary>
        /// Gets the execution errors
        /// </summary>
        IExecutionErrors Errors { get; }
    }
}

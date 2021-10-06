using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.GraphQL
{
    /// <summary>
    /// GraphQL.Language.AST.Operation interface for ducktyping
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IOperation
    {
        /// <summary>
        /// Gets the name of the operation
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the operation
        /// </summary>
        OperationTypeProxy OperationType { get; }
    }
}

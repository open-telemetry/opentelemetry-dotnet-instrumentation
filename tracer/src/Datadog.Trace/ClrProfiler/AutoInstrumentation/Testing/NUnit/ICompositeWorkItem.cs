using System.Collections;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// DuckTyping interface for NUnit.Framework.Internal.Execution.CompositeWorkItem
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ICompositeWorkItem : IWorkItem
    {
        /// <summary>
        /// Gets the List of Child WorkItems
        /// </summary>
        IEnumerable Children { get; }
    }
}

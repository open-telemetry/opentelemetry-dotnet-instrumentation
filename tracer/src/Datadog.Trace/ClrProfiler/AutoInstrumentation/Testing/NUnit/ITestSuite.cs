using System.Collections;
using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// DuckTyping interface for NUnit.Framework.Internal.TestSuite
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITestSuite : ITest
    {
        /// <summary>
        /// Gets the children tests
        /// </summary>
        IEnumerable Tests { get; }
    }
}

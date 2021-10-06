using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// DuckTyping interface for NUnit.Framework.Interfaces.ResultState
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IResultState
    {
        /// <summary>
        /// Gets the TestStatus for the test.
        /// </summary>
        /// <value>The status.</value>
        TestStatus Status { get; }

        /// <summary>
        /// Gets the stage of test execution in which
        /// the failure or other result took place.
        /// </summary>
        FailureSite Site { get; }
    }
}

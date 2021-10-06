using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestMethodRunner ducktype interface
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITestMethodRunner
    {
        /// <summary>
        /// Gets the TestMethodInfo instance
        /// </summary>
        [DuckField(Name = "testMethodInfo")]
        ITestMethod TestMethodInfo { get; }
    }
}

using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// The TestStatus enum indicates the result of running a test
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum TestStatus
    {
        /// <summary>
        /// The test was inconclusive
        /// </summary>
        Inconclusive,

        /// <summary>
        /// The test has skipped
        /// </summary>
        Skipped,

        /// <summary>
        /// The test succeeded
        /// </summary>
        Passed,

        /// <summary>
        /// There was a warning
        /// </summary>
        Warning,

        /// <summary>
        /// The test failed
        /// </summary>
        Failed
    }
}

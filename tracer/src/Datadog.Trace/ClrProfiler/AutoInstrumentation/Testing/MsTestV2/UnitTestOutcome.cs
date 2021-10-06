using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// Unit test outcomes
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum UnitTestOutcome : int
    {
        /// <summary>
        /// Test was executed, but there were issues.
        /// Issues may involve exceptions or failed assertions.
        /// </summary>
        Failed,

        /// <summary>
        /// Test has completed, but we can't say if it passed or failed.
        /// May be used for aborted tests.
        /// </summary>
        Inconclusive,

        /// <summary>
        /// Test was executed without any issues.
        /// </summary>
        Passed,

        /// <summary>
        /// Test is currently executing.
        /// </summary>
        InProgress,

        /// <summary>
        /// There was a system error while we were trying to execute a test.
        /// </summary>
        Error,

        /// <summary>
        /// The test timed out.
        /// </summary>
        Timeout,

        /// <summary>
        /// Test was aborted by the user.
        /// </summary>
        Aborted,

        /// <summary>
        /// Test is in an unknown state
        /// </summary>
        Unknown,

        /// <summary>
        /// Test cannot be executed.
        /// </summary>
        NotRunnable
    }
}

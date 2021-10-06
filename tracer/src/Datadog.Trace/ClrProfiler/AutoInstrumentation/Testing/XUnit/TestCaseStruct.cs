using System.Collections.Generic;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit
{
    /// <summary>
    /// TestCase structure
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct TestCaseStruct
    {
        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Traits dictionary
        /// </summary>
        public Dictionary<string, List<string>> Traits;
    }
}

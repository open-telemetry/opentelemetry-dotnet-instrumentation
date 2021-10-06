using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestPropertyAttribute ducktype struct
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct TestPropertyAttributeStruct
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value;
    }
}

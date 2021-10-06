using System.Collections.Generic;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestCategoryAttribute ducktype struct
    /// </summary>
    [DuckCopy]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct TestCategoryAttributeStruct
    {
        /// <summary>
        /// Gets the test categories
        /// </summary>
        public IList<string> TestCategories;
    }
}

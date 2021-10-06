using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Datadog.Trace.AppSec.Waf.NativeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DdwafResultStruct
    {
        [Obsolete("This member will be removed from then ddwaf library vby a future PR")]
        public DDWAF_RET_CODE Action;

        public IntPtr Data;

        public IntPtr PerfData;

        public int PerfTotalRuntime;

        public int PerfCacheHitRate;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.AppSec.Waf.NativeBindings
{
    internal enum DDWAF_RET_CODE
    {
        DDWAF_ERR_INTERNAL = -4,
        DDWAF_ERR_INVALID_OBJECT = -3,
        DDWAF_ERR_INVALID_ARGUMENT = -2,
        DDWAF_ERR_TIMEOUT = -1,
        DDWAF_GOOD = 0,
        DDWAF_MONITOR = 1,
        DDWAF_BLOCK = 2
    }
}

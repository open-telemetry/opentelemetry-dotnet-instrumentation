using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.AppSec.Waf
{
    internal enum ReturnCode
    {
        ErrorInternal = -4,
        ErrorInvalidObject = -3,
        ErrorInvalidArgument = -2,
        ErrorTimeout = -1,
        Good = 0,
        Monitor = 1,
        Block = 2
    }
}

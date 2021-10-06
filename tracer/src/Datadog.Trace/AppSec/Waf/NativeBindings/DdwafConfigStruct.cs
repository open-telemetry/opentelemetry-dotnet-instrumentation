using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.AppSec.Waf.NativeBindings
{
    internal struct DdwafConfigStruct
    {
        public ulong MaxArrayLength;

        public ulong MaxMapDepth;

        public int MaxTimeStore;
    }
}

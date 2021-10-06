using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Datadog.Trace.AppSec.Waf.NativeBindings
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DdwafVersionStruct
    {
        public ushort Major;

        public ushort Minor;

        public ushort Patch;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}

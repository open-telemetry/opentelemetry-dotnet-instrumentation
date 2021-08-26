using System;
using System.Runtime.InteropServices;

namespace OpenTelemetry.ClrProfiler.Managed.Util
{
    internal static class PointerHelpers
    {
        public static Guid GetGuidFromNativePointer(long nativePointer)
        {
            var ptr = new IntPtr(nativePointer);
            var moduleVersionId = Marshal.PtrToStructure<Guid>(ptr);
            return moduleVersionId;
        }
    }
}

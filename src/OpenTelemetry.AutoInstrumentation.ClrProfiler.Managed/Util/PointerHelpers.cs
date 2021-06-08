using System;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util
{
    internal static class PointerHelpers
    {
        public static Guid GetGuidFromNativePointer(long nativePointer)
        {
            var ptr = new IntPtr(nativePointer);
#if NET452
            // deprecated
            var moduleVersionId = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
#else
            // added in net451
            var moduleVersionId = Marshal.PtrToStructure<Guid>(ptr);
#endif
            return moduleVersionId;
        }
    }
}

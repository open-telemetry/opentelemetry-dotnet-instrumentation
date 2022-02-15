using System;
using System.Runtime.InteropServices;

namespace OpenTelemetry.ClrProfiler.Managed
{
    internal static class NativeMethods
    {
        private static readonly bool IsWindows = string.Equals(FrameworkDescription.Instance.OSPlatform, "Windows", StringComparison.OrdinalIgnoreCase);

        public static bool IsProfilerAttached()
        {
            if (IsWindows)
            {
                return Windows.IsProfilerAttached();
            }

            return NonWindows.IsProfilerAttached();
        }

        // the "dll" extension is required on .NET Framework
        // and optional on .NET Core
        private static class Windows
        {
            [DllImport("OpenTelemetry.ClrProfiler.Native.dll")]
            public static extern bool IsProfilerAttached();
        }

        // assume .NET Core if not running on Windows
        private static class NonWindows
        {
            [DllImport("OpenTelemetry.ClrProfiler.Native")]
            public static extern bool IsProfilerAttached();
        }
    }
}

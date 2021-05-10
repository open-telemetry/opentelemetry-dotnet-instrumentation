using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace Datadog.Trace.Util
{
    internal static class ExceptionUtil
    {
        public static bool IsAssemblyLoadException(Exception ex)
        {
            return ex is ArgumentNullException
                or FileNotFoundException
                or FileLoadException
                or BadImageFormatException
                or SecurityException
                or ArgumentException
                or PathTooLongException;
        }

        public static bool IsDynamicInvocationException(Exception ex)
        {
            return ex is ReflectionTypeLoadException
                or ArgumentNullException
                or ArgumentException
                or NotSupportedException
                or TargetInvocationException
                or MethodAccessException
                or MemberAccessException
                or InvalidComObjectException
                or MissingMethodException
                or COMException
                or TypeLoadException;
        }
    }
}

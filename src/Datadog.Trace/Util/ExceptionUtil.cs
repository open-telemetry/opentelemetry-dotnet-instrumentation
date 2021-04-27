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
            return
                ex is ArgumentNullException ||
                ex is FileNotFoundException ||
                ex is FileLoadException ||
                ex is BadImageFormatException ||
                ex is SecurityException ||
                ex is ArgumentException ||
                ex is PathTooLongException;
        }

        public static bool IsDynamicInvocationException(Exception ex)
        {
            return
                ex is ReflectionTypeLoadException ||
                ex is ArgumentNullException ||
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is TargetInvocationException ||
                ex is MethodAccessException ||
                ex is MemberAccessException ||
                ex is InvalidComObjectException ||
                ex is MissingMethodException ||
                ex is COMException ||
                ex is TypeLoadException;
        }
    }
}

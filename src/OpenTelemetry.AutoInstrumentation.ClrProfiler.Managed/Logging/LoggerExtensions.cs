using System;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Logging
{
    internal static class LoggerExtensions
    {
        public static void ErrorRetrievingMethod(
            this ILogger logger,
            Exception exception,
            long moduleVersionPointer,
            int mdToken,
            int opCode,
            string instrumentedType,
            string methodName,
            string instanceType = null,
            string[] relevantArguments = null,
            [CallerLineNumber] int sourceLine = 0,
            [CallerFilePath] string sourceFile = "")
        {
            var instrumentedMethod = $"{instrumentedType}.{methodName}(...)";

            if (instanceType != null)
            {
                instrumentedMethod = $"{instrumentedMethod} on {instanceType}";
            }

            if (relevantArguments != null)
            {
                instrumentedMethod = $"{instrumentedMethod} with {string.Join(", ", relevantArguments)}";
            }

            var moduleVersionId = PointerHelpers.GetGuidFromNativePointer(moduleVersionPointer);

            // ReSharper disable twice ExplicitCallerInfoArgument
            logger.Error(
                exception,
                $"Error (MVID: {moduleVersionId}, mdToken: {mdToken}, opCode: {opCode}) could not retrieve: {instrumentedMethod}",
                sourceLine,
                sourceFile);
        }
    }
}

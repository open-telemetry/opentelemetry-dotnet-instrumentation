using System;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers
{
    internal static class IntegrationOptions<TIntegration, TTarget>
    {
        private static readonly ILogger Log = ConsoleLogger.Create(typeof(IntegrationOptions<TIntegration, TTarget>));

        private static volatile bool _disableIntegration = false;

        internal static bool IsIntegrationEnabled => !_disableIntegration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisableIntegration() => _disableIntegration = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogException(Exception exception, string message = null)
        {
            // ReSharper disable twice ExplicitCallerInfoArgument
            Log.Error(exception, message ?? exception?.Message);
            if (exception is DuckTypeException)
            {
                Log.Warning($"DuckTypeException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }
            else if (exception is CallTargetInvokerException)
            {
                Log.Warning($"CallTargetInvokerException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }
        }
    }
}

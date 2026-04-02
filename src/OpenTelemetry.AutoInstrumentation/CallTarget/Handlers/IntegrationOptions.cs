// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class IntegrationOptions<TIntegration, TTarget>
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    private static volatile bool _disableIntegration;

    internal static bool IsIntegrationEnabled => !_disableIntegration;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DisableIntegration() => _disableIntegration = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogException(Exception exception, string? message = null)
    {
        // ReSharper disable twice ExplicitCallerInfoArgument
        Log.Error(exception, message ?? exception.Message);
        if (exception is DuckTypeException or TargetInvocationException { InnerException: DuckTypeException })
        {
            Log.Warning($"DuckTypeException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
            _disableIntegration = true;
        }
        else if (exception is CallTargetInvokerException)
        {
            Log.Warning($"CallTargetInvokerException has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
            _disableIntegration = true;
        }
        else if (exception is FileLoadException fileLoadException)
        {
            if (fileLoadException.FileName != null && (fileLoadException.FileName.StartsWith("System.Diagnostics.DiagnosticSource", StringComparison.Ordinal) || fileLoadException.FileName.StartsWith("System.Runtime.CompilerServices.Unsafe", StringComparison.Ordinal)))
            {
                Log.Warning($"FileLoadException for '{fileLoadException.FileName}' has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }
        }
    }
}

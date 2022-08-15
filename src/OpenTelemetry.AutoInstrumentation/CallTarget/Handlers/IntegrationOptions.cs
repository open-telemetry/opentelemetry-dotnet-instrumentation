// <copyright file="IntegrationOptions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.IO;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class IntegrationOptions<TIntegration, TTarget>
{
    private static readonly ILogger Log = OtelLogging.GetLogger();

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
        else if (exception is FileLoadException fileLoadException)
        {
            if (fileLoadException.FileName.StartsWith("System.Diagnostics.DiagnosticSource") || fileLoadException.FileName.StartsWith("System.Runtime.CompilerServices.Unsafe"))
            {
                Log.Warning($"FileLoadException for '{fileLoadException.FileName}' has been detected, the integration <{typeof(TIntegration)}, {typeof(TTarget)}> will be disabled.");
                _disableIntegration = true;
            }
        }
    }
}

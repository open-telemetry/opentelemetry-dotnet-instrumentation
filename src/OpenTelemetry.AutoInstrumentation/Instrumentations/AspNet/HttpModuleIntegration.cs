// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Web;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.Instrumentation.AspNet;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AspNet;

/// <summary>
/// System.Web.Compilation.BuildManager.InvokePreStartInitMethodsCore calltarget instrumentation
/// </summary>
[InstrumentMethod(
    "System.Web",
    "System.Web.Compilation.BuildManager",
    "InvokePreStartInitMethodsCore",
    ClrNames.Void,
    ["System.Collections.Generic.ICollection`1[System.Reflection.MethodInfo]", "System.Func`1[System.IDisposable]"],
    "4.0.0",
    "4.*.*",
    "AspNet",
    InstrumentationType.Trace)]
public static class HttpModuleIntegration
{
    private static int _initialized;

    internal static bool IsInitialized => Interlocked.CompareExchange(ref _initialized, 0, 0) != default;

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TCollection">Type of the collection</typeparam>
    /// <typeparam name="TFunc">Type of the </typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method. This method is static so this parameter will always be null</param>
    /// <param name="methods">The methods to be invoked</param>
    /// <param name="setHostingEnvironmentCultures">The function to set the environment culture</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TCollection, TFunc>(TTarget instance, TCollection methods, TFunc setHostingEnvironmentCultures)
    {
        if (Interlocked.Exchange(ref _initialized, 1) != default)
        {
            return CallTargetState.GetDefault();
        }

        try
        {
            HttpApplication.RegisterModule(typeof(TelemetryHttpModule));
        }
#pragma warning disable CA1031 // Do not catch general exception types. Intentionally catching all exceptions to avoid breaking the application
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types. Intentionally catching all exceptions to avoid breaking the application
        {
            // Exception while registering telemetry http module
            // nothing we can do with this
        }

        return CallTargetState.GetDefault();
    }
}
#endif

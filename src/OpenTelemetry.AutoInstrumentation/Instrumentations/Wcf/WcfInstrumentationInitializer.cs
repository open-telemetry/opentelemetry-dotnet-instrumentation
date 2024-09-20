// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Instrumentations.AspNet;
#endif
using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf;

internal static class WcfInstrumentationInitializer
{
    private static int _instrumentationOptionsInitialized;
#if NETFRAMEWORK
    private static int _aspNetParentSpanCorrectorInitialized;
#endif

    public static void TryInitializeOptions()
    {
        if (Interlocked.Exchange(ref _instrumentationOptionsInitialized, value: 1) != default)
        {
            return;
        }

        var options = new WcfInstrumentationOptions();

        Instrumentation.PluginManager?.ConfigureTracesOptions(options);

        var instrumentationType =
            Type.GetType(
                "OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationActivitySource, OpenTelemetry.Instrumentation.Wcf");

        instrumentationType?.GetProperty("Options")?.SetValue(null, options);
    }

#if NETFRAMEWORK
    public static void TryInitializeParentSpanCorrector()
    {
        if (Interlocked.Exchange(ref _aspNetParentSpanCorrectorInitialized, value: 1) != default)
        {
            return;
        }

        if (HttpModuleIntegration.IsInitialized)
        {
            var aspNetParentSpanCorrectorType = Type.GetType("OpenTelemetry.Instrumentation.Wcf.Implementation.AspNetParentSpanCorrector, OpenTelemetry.Instrumentation.Wcf");
            var methodInfo = aspNetParentSpanCorrectorType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            methodInfo?.Invoke(null, null);
        }
    }
#endif
}

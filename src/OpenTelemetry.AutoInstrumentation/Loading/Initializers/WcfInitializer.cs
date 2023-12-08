// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class WcfInitializer : InstrumentationInitializer
{
    private readonly PluginManager _pluginManager;

    public WcfInitializer(PluginManager pluginManager)
#if NETFRAMEWORK
        : base("System.ServiceModel")
#else
        : base("System.ServiceModel.Primitives")
#endif
    {
        _pluginManager = pluginManager;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        var options = new WcfInstrumentationOptions();

        _pluginManager.ConfigureTracesOptions(options);

        var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationActivitySource, OpenTelemetry.Instrumentation.Wcf");

        instrumentationType?.GetProperty("Options")?.SetValue(null, options);

#if NETFRAMEWORK
        var enabledTraceInstrumentations = Instrumentation.TracerSettings.Value.EnabledInstrumentations;
        if (enabledTraceInstrumentations.Contains(TracerInstrumentation.WcfService) && enabledTraceInstrumentations.Contains(TracerInstrumentation.AspNet))
        {
            var aspNetParentSpanCorrectorType = Type.GetType("OpenTelemetry.Instrumentation.Wcf.Implementation.AspNetParentSpanCorrector, OpenTelemetry.Instrumentation.Wcf");
            var methodInfo = aspNetParentSpanCorrectorType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            methodInfo?.Invoke(null, null);
        }
#endif
    }
}

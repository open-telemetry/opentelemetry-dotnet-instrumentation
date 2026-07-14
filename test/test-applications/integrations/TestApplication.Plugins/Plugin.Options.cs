// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Options extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin,
    IConfigureTracesOptions<HttpClientTraceInstrumentationOptions>,
    IConfigureTracesOptions<OtlpExporterOptions>,
    IConfigureMetricsOptions<OtlpExporterOptions>
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public void ConfigureTracesOptions(HttpClientTraceInstrumentationOptions options)
#pragma warning restore CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    {
        ThrowIfMissing(options);

#if NETFRAMEWORK
        options.EnrichWithHttpWebRequest = (activity, message) =>
#else
        options.EnrichWithHttpRequestMessage = (activity, message) =>
#endif
        {
            activity.SetTag("example.plugin", "MyExamplePlugin");
        };
    }

    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureTracesOptions)}({nameof(OtlpExporterOptions)} {nameof(options)}) invoked.");
    }

    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureMetricsOptions)}({nameof(OtlpExporterOptions)} {nameof(options)}) invoked.");
    }
}

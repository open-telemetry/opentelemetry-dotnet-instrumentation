// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace TestApplication.Plugins;

public class Plugin
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initializing)}() invoked.");
    }

    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder.AddSource(TestApplication.Smoke.Program.SourceName);
    }

    public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder.AddMeter(TestApplication.Smoke.Program.SourceName);
    }

    public void ConfigureTracesOptions(HttpClientTraceInstrumentationOptions options)
    {
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

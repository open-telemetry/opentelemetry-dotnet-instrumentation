// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TestApplication.Smoke;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Core of the plugin.
/// </summary>
public partial class Plugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initializing)}() invoked.");
    }

    public void Initialized()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initialized)}() invoked.");
    }

#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
#if NET
        ArgumentNullException.ThrowIfNull(builder);
#else
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
#endif

        return builder.AddSource(Program.SourceName);
    }

    public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
#if NET
        ArgumentNullException.ThrowIfNull(builder);
#else
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
#endif

        return builder.AddMeter(Program.SourceName);
    }

    public void ConfigureTracesOptions(HttpClientTraceInstrumentationOptions options)
#pragma warning restore CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    {
#if NET
        ArgumentNullException.ThrowIfNull(options);
#else
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
#endif

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

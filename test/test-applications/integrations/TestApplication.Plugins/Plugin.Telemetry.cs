// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TestApplication.Smoke;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Telemetry extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, ITelemetryPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
#pragma warning disable CA1822 // Mark members as static. Needed for AutoInstrumentation plugin loading.
    public TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        ThrowIfMissing(builder);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeConfigureTracerProvider)}() invoked.");

        return builder.AddSource(Program.SourceName);
    }

    public MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder)
    {
        ThrowIfMissing(builder);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeConfigureMeterProvider)}() invoked.");

        return builder.AddMeter(Program.SourceName);
    }

    public void TracerProviderInitialized(TracerProvider tracerProvider)
    {
        ThrowIfMissing(tracerProvider);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(TracerProviderInitialized)}() invoked.");
    }

    public void MeterProviderInitialized(MeterProvider meterProvider)
    {
        ThrowIfMissing(meterProvider);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(MeterProviderInitialized)}() invoked.");
    }

    public TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder)
    {
        ThrowIfMissing(builder);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterConfigureTracerProvider)}() invoked.");

        return builder;
    }

    public MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder)
    {
        ThrowIfMissing(builder);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterConfigureMeterProvider)}() invoked.");

        return builder;
    }

    public ResourceBuilder ConfigureResource(ResourceBuilder builder)
    {
        ThrowIfMissing(builder);

        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureResource)}() invoked.");

        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfMissing<T>(T input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(input);
#else
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }
#endif
    }
}

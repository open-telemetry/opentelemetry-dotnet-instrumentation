// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.Telemetry;

/// <summary>
/// Provides extension points for customizing OpenTelemetry setup and runtime behavior.
/// </summary>
public interface ITelemetryPlugin
{
    /// <summary>
    /// Called after the <see cref="TracerProvider"/> has been fully built and started.
    /// </summary>
    /// <param name="tracerProvider">The initialized tracer provider.</param>
    void TracerProviderInitialized(TracerProvider tracerProvider);

    /// <summary>
    /// Called after the <see cref="MeterProvider"/> has been fully built and started.
    /// </summary>
    /// <param name="meterProvider">The initialized meter provider.</param>
    void MeterProviderInitialized(MeterProvider meterProvider);

    /// <summary>
    /// Allows modification of the <see cref="TracerProviderBuilder"/> before core configuration is applied.
    /// </summary>
    /// <param name="builder">The tracer provider builder to configure.</param>
    /// <returns>The configured tracer provider builder.</returns>
    TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="MeterProviderBuilder"/> before core configuration is applied.
    /// </summary>
    /// <param name="builder">The meter provider builder to configure.</param>
    /// <returns>The configured meter provider builder.</returns>
    MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="TracerProviderBuilder"/> after core configuration has been applied,
    /// but before the provider is built.
    /// </summary>
    /// <param name="builder">The tracer provider builder to configure.</param>
    /// <returns>The configured tracer provider builder.</returns>
    TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="MeterProviderBuilder"/> after core configuration has been applied,
    /// but before the provider is built.
    /// </summary>
    /// <param name="builder">The meter provider builder to configure.</param>
    /// <returns>The configured meter provider builder.</returns>
    MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder);

    /// <summary>
    /// Allows modification or enrichment of the <see cref="ResourceBuilder"/> used for telemetry metadata.
    /// </summary>
    /// <param name="builder">The resource builder to configure.</param>
    /// <returns>The configured resource builder.</returns>
    ResourceBuilder ConfigureResource(ResourceBuilder builder);
}

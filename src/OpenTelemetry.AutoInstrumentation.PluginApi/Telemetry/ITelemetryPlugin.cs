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
    void TracerProviderInitialized(TracerProvider tracerProvider);

    /// <summary>
    /// Called after the <see cref="MeterProvider"/> has been fully built and started.
    /// </summary>
    void MeterProviderInitialized(MeterProvider meterProvider);

    /// <summary>
    /// Allows modification of the <see cref="TracerProviderBuilder"/> before core configuration is applied.
    /// </summary>
    TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="MeterProviderBuilder"/> before core configuration is applied.
    /// </summary>
    MeterProviderBuilder BeforeConfigureMeterProvider(MeterProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="TracerProviderBuilder"/> after core configuration has been applied,
    /// but before the provider is built.
    /// </summary>
    TracerProviderBuilder AfterConfigureTracerProvider(TracerProviderBuilder builder);

    /// <summary>
    /// Allows modification of the <see cref="MeterProviderBuilder"/> after core configuration has been applied,
    /// but before the provider is built.
    /// </summary>
    MeterProviderBuilder AfterConfigureMeterProvider(MeterProviderBuilder builder);

    /// <summary>
    /// Allows modification or enrichment of the <see cref="ResourceBuilder"/> used for telemetry metadata.
    /// </summary>
    ResourceBuilder ConfigureResource(ResourceBuilder builder);
}

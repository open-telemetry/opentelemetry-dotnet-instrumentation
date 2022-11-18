// <copyright file="Instrumentation.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics.Tracing;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;
using OpenTracing.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
internal static class Instrumentation
{
    private static readonly ILogger Logger = OtelLogging.GetLogger();
    private static readonly LazyInstrumentationLoader LazyInstrumentationLoader = new();

    private static int _firstInitialization = 1;
    private static int _isExiting = 0;
    private static SdkSelfDiagnosticsEventListener _sdkEventListener;

    private static TracerProvider _tracerProvider;
    private static MeterProvider _meterProvider;
    private static PluginManager _pluginManager;

    /// <summary>
    /// Gets a value indicating whether OpenTelemetry's profiler is attached to the current process.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the profiler is currently attached; <c>false</c> otherwise.
    /// </value>
    public static bool ProfilerAttached
    {
        get
        {
            try
            {
                return NativeMethods.IsProfilerAttached();
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }
    }

    internal static PluginManager PluginManager => _pluginManager;

    internal static ILifespanManager LifespanManager => LazyInstrumentationLoader.LifespanManager;

    internal static GeneralSettings GeneralSettings { get; } = Settings.FromDefaultSources<GeneralSettings>();

    internal static TracerSettings TracerSettings { get; } = Settings.FromDefaultSources<TracerSettings>();

    internal static MetricSettings MetricSettings { get; } = Settings.FromDefaultSources<MetricSettings>();

    internal static LogSettings LogSettings { get; } = Settings.FromDefaultSources<LogSettings>();

    internal static SdkSettings SdkSettings { get; } = Settings.FromDefaultSources<SdkSettings>();

    /// <summary>
    /// Initialize the OpenTelemetry SDK with a pre-defined set of exporters, shims, and
    /// instrumentations.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _firstInitialization, value: 0) != 1)
        {
            // Initialize() was already called before
            return;
        }

        try
        {
            _pluginManager = new PluginManager(GeneralSettings);

            if (TracerSettings.TracesEnabled || MetricSettings.MetricsEnabled)
            {
                // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
                _sdkEventListener = new(EventLevel.Warning, Logger);

                // Register to shutdown events
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                AppDomain.CurrentDomain.DomainUnload += OnExit;

                if (GeneralSettings.FlushOnUnhandledException)
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                }

                EnvironmentConfigurationSdkHelper.UseEnvironmentVariables(SdkSettings);
            }

            if (TracerSettings.TracesEnabled)
            {
                var builder = Sdk
                    .CreateTracerProviderBuilder()
                    .SetResourceBuilder(ResourceFactory.Create())
                    .UseEnvironmentVariables(LazyInstrumentationLoader, TracerSettings, _pluginManager)
                    .InvokePlugins(_pluginManager);

                _tracerProvider = builder.Build();
                Logger.Information("OpenTelemetry tracer initialized.");
            }

            if (MetricSettings.MetricsEnabled)
            {
                var builder = Sdk
                    .CreateMeterProviderBuilder()
                    .SetResourceBuilder(ResourceFactory.Create())
                    .UseEnvironmentVariables(LazyInstrumentationLoader, MetricSettings, _pluginManager)
                    .InvokePlugins(_pluginManager);

                _meterProvider = builder.Build();
                Logger.Information("OpenTelemetry meter initialized.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTelemetry SDK load exception.");
            throw;
        }

        if (TracerSettings.OpenTracingEnabled)
        {
            EnableOpenTracing();
        }
    }

    private static void OnExit(object sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
        {
            // OnExit() was already called before
            return;
        }

        try
        {
#if NET6_0_OR_GREATER
            LazyInstrumentationLoader?.Dispose();
#endif
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            _sdkEventListener.Dispose();

            Logger.Information("OpenTelemetry Automatic Instrumentation exit.");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An error occurred while attempting to exit.");
            }
            catch
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        try
        {
            if (args.IsTerminating)
            {
                Logger.Error("UnhandledException event raised with a terminating exception.");
                OnExit(sender, args);
            }
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An exception occurred while processing an unhandled exception.");
            }
            catch
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
    }

    private static void EnableOpenTracing()
    {
        try
        {
            if (_tracerProvider is not null)
            {
                // Instantiate the OpenTracing shim. The underlying OpenTelemetry tracer will create
                // spans using the "OpenTelemetry.AutoInstrumentation.OpenTracingShim" source.
                var openTracingShim = new TracerShim(
                    _tracerProvider.GetTracer("OpenTelemetry.AutoInstrumentation.OpenTracingShim"),
                    Propagators.DefaultTextMapPropagator);

                // This registration must occur prior to any reference to the OpenTracing tracer:
                // otherwise the no-op tracer is going to be used by OpenTracing instead.
                GlobalTracer.RegisterIfAbsent(openTracingShim);
                Logger.Information("OpenTracingShim loaded.");
            }
            else
            {
                Logger.Information("OpenTracingShim was not loaded as the provider is not initialized.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTracingShim exception.");
            throw;
        }
    }
}

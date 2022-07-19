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
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;
using OpenTracing.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
public static class Instrumentation
{
    private static readonly ILogger Logger = OtelLogging.GetLogger();
    private static readonly ResourceBuilder _resourceBuilder = ResourceBuilder.CreateDefault();
    private static readonly LazyInstrumentationLoader LazyInstrumentationLoader = new();

    private static int _firstInitialization = 1;
    private static int _isExiting = 0;
    private static SdkSelfDiagnosticsEventListener _sdkEventListener;

    private static TracerProvider _tracerProvider;
    private static MeterProvider _meterProvider;

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

    internal static TracerSettings TracerSettings { get; } = TracerSettings.FromDefaultSources();

    internal static MeterSettings MeterSettings { get; } = MeterSettings.FromDefaultSources();

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
            if (TracerSettings.LoadTracerAtStartup || MeterSettings.LoadMetricsAtStartup)
            {
                // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
                _sdkEventListener = new(EventLevel.Warning, Logger);

                // Register to shutdown events
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                AppDomain.CurrentDomain.DomainUnload += OnExit;

                if (TracerSettings.FlushOnUnhandledException || MeterSettings.FlushOnUnhandledException)
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                }
            }

            if (TracerSettings.LoadTracerAtStartup)
            {
                // Setup the instrumentations that have additional setup occurring during AssemblyLoad
                // -> this should be refactored in a separate PR
                // e.g. we could have a static method that returns a collection of initializers
                //      and TracerSettings.EnabledInstrumentations would be passed as input
                if (TracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.MySqlData))
                {
                    LazyInstrumentationLoader.Add(new MySqlDataInitializer());
                }

                var builder = Sdk
                    .CreateTracerProviderBuilder()
                    .SetResourceBuilder(_resourceBuilder)
                    .UseEnvironmentVariables(TracerSettings)
                    .SetSampler(new AlwaysOnSampler())
                    .InvokePlugins(TracerSettings.TracerPlugins);

                _tracerProvider = builder.Build();
                Logger.Information("OpenTelemetry tracer initialized.");
            }

            if (MeterSettings.LoadMetricsAtStartup)
            {
                var builder = Sdk
                    .CreateMeterProviderBuilder()
                    .SetResourceBuilder(_resourceBuilder)
                    .UseEnvironmentVariables(MeterSettings)
                    .InvokePlugins(MeterSettings.MetricPlugins);

                _meterProvider = builder.Build();
                Logger.Information("OpenTelemetry meter initialized.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTelemetry SDK load exception.");
            throw;
        }

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

    private static void OnExit(object sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
        {
            // OnExit() was already called before
            return;
        }

        try
        {
            LazyInstrumentationLoader?.Dispose();
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            _sdkEventListener.Dispose();

            Logger.Information("OpenTelemetry Automatic Instrumentation exit.");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An error occured while attempting to exit.");
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
}

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
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
public static class Instrumentation
{
    private static readonly Process _process = Process.GetCurrentProcess();
    private static int _firstInitialization = 1;
    private static int _isExiting = 0;
    private static SdkSelfDiagnosticsEventListener _sdkEventListener;

    private static TracerProvider _tracerProvider;

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

    internal static Settings TracerSettings { get; } = Settings.FromDefaultSources();

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
            if (TracerSettings.LoadTracerAtStartup)
            {
                // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
                _sdkEventListener = new(EventLevel.Warning);

                var builder = Sdk
                    .CreateTracerProviderBuilder()
                    .UseEnvironmentVariables(TracerSettings)
                    .SetSampler(new AlwaysOnSampler())
                    .InvokePlugins(TracerSettings.TracerPlugins);

                _tracerProvider = builder.Build();
                Log("OpenTelemetry tracer initialized.");

                // Register to shutdown events
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                AppDomain.CurrentDomain.DomainUnload += OnExit;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }
        }
        catch (Exception ex)
        {
            Log($"OpenTelemetry SDK load exception: {ex}");
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
                OpenTracing.Util.GlobalTracer.RegisterIfAbsent(openTracingShim);
                Log("OpenTracingShim loaded.");
            }
            else
            {
                Log("OpenTracingShim was not loaded as the provider is not initialized.");
            }
        }
        catch (Exception ex)
        {
            Log($"OpenTracingShim exception: {ex}");
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
            _tracerProvider.Dispose();
            _sdkEventListener.Dispose();

            Log("OpenTelemetry tracer exit.");
        }
        catch (Exception ex)
        {
            try
            {
                Log($"An error occured while attempting to exit. {ex}");
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
                Log("UnhandledException event raised with a terminating exception.");
                OnExit(sender, args);
            }
        }
        catch (Exception ex)
        {
            try
            {
                Log($"An exception occured while processing an unhandled exception. {ex}");
            }
            catch
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Process: {_process.ProcessName}({_process.Id}): {message}");
    }
}

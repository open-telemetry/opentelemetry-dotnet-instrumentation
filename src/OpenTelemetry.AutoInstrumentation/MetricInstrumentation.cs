// <copyright file="MetricInstrumentation.cs" company="OpenTelemetry Authors">
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
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
public static class MetricInstrumentation
{
    private static readonly Process _process = Process.GetCurrentProcess();
    private static int _firstInitialization = 1;
    private static int _isExiting = 0;

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

    internal static MetricSettings MetricSettings { get; } = MetricSettings.FromDefaultSources();

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
            if (MetricSettings.LoadMetricsAtStartup)
            {
                var builder = Sdk
                    .CreateMeterProviderBuilder()
                    .UseEnvironmentVariables(MetricSettings)
                    // .AddConsoleExporter()
                    // .AddMeter("MyCompany.MyProduct.MyLibrary")
                    .InvokePlugins(MetricSettings.MetricPlugins);

                _meterProvider = builder.Build();
                Log("OpenTelemetry metrics initialized.");

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
            _meterProvider.Dispose();

            Log("OpenTelemetry meter exit.");
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

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

using OpenTelemetry.AutoInstrumentation.Configurations;
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
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly LazyInstrumentationLoader LazyInstrumentationLoader = new();

    private static int _initialized;
    private static int _isExiting;
    private static SdkSelfDiagnosticsEventListener? _sdkEventListener;

    private static TracerProvider? _tracerProvider;
    private static MeterProvider? _meterProvider;
    private static PluginManager? _pluginManager;

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

    internal static PluginManager? PluginManager => _pluginManager;

    internal static ILifespanManager LifespanManager => LazyInstrumentationLoader.LifespanManager;

    internal static Lazy<GeneralSettings> GeneralSettings { get; } = new(Settings.FromDefaultSources<GeneralSettings>);

    internal static Lazy<TracerSettings> TracerSettings { get; } = new(Settings.FromDefaultSources<TracerSettings>);

    internal static Lazy<MetricSettings> MetricSettings { get; } = new(Settings.FromDefaultSources<MetricSettings>);

    internal static Lazy<LogSettings> LogSettings { get; } = new(Settings.FromDefaultSources<LogSettings>);

    internal static Lazy<SdkSettings> SdkSettings { get; } = new(Settings.FromDefaultSources<SdkSettings>);

    /// <summary>
    /// Initialize the OpenTelemetry SDK with a pre-defined set of exporters, shims, and
    /// instrumentations.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // Initialize() was already called before
            return;
        }

#if NETFRAMEWORK
        try
        {
            // On .NET Framework only, initialize env vars from app.config/web.config
            // this does not override settings which where already set via env vars.
            // We are doing so as the OTel .NET SDK only supports the env vars and we want to be
            // be able to set them via app.config/web.config.
            EnvironmentInitializer.Initialize(System.Configuration.ConfigurationManager.AppSettings);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize from AppSettings.");
            throw;
        }
#endif

        try
        {
            _pluginManager = new PluginManager(GeneralSettings.Value);
            _pluginManager.Initializing();

            if (TracerSettings.Value.TracesEnabled || MetricSettings.Value.MetricsEnabled)
            {
                // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
                _sdkEventListener = new(Logger);

                // Register to shutdown events
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                AppDomain.CurrentDomain.DomainUnload += OnExit;

                if (GeneralSettings.Value.FlushOnUnhandledException)
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                }

                EnvironmentConfigurationSdkHelper.UseEnvironmentVariables(SdkSettings.Value);
            }

            if (TracerSettings.Value.TracesEnabled)
            {
                if (GeneralSettings.Value.SetupSdk)
                {
                    var builder = Sdk
                        .CreateTracerProviderBuilder()
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder())
                        .UseEnvironmentVariables(LazyInstrumentationLoader, TracerSettings.Value, _pluginManager)
                        .InvokePlugins(_pluginManager);

                    _tracerProvider = builder.Build();
                    Logger.Information("OpenTelemetry tracer initialized.");
                }
                else
                {
                    AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader, _pluginManager, TracerSettings.Value.EnabledInstrumentations);
                    Logger.Information("Initialized lazily-loaded trace instrumentations without initializing sdk.");
                }
            }

            if (MetricSettings.Value.MetricsEnabled)
            {
                if (GeneralSettings.Value.SetupSdk)
                {
                    var builder = Sdk
                        .CreateMeterProviderBuilder()
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder())
                        .UseEnvironmentVariables(LazyInstrumentationLoader, MetricSettings.Value, _pluginManager)
                        .InvokePlugins(_pluginManager);

                    _meterProvider = builder.Build();
                    Logger.Information("OpenTelemetry meter initialized.");
                }
                else
                {
                    AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader, MetricSettings.Value.EnabledInstrumentations);

                    Logger.Information("Initialized lazily-loaded metric instrumentations without initializing sdk.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTelemetry SDK load exception.");
            throw;
        }

        if (TracerSettings.Value.OpenTracingEnabled)
        {
            EnableOpenTracing();
        }
    }

    private static void AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, IList<MetricInstrumentation> enabledInstrumentations)
    {
        foreach (var instrumentation in enabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case MetricInstrumentation.AspNet:
                    DelayedInitialization.Metrics.AddAspNet(lazyInstrumentationLoader);
                    break;
#endif
#if NET6_0_OR_GREATER
                case MetricInstrumentation.AspNetCore:
                    DelayedInitialization.Metrics.AddAspNetCore(lazyInstrumentationLoader);
                    break;
#endif
                case MetricInstrumentation.HttpClient:
                    DelayedInitialization.Metrics.AddHttpClient(lazyInstrumentationLoader);
                    break;
                case MetricInstrumentation.NetRuntime:
                    break;
                case MetricInstrumentation.Process:
                    break;
                case MetricInstrumentation.NServiceBus:
                    break;
                default:
                    Logger.Warning($"Configured metric instrumentation type is not supported: {instrumentation}");
                    break;
            }
        }
    }

    private static void AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, IList<TracerInstrumentation> enabledInstrumentations)
    {
        foreach (var instrumentation in enabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case TracerInstrumentation.AspNet:
                    DelayedInitialization.Traces.AddAspNet(lazyInstrumentationLoader, pluginManager);
                    break;
#endif
                case TracerInstrumentation.HttpClient:
                    DelayedInitialization.Traces.AddHttpClient(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.GrpcNetClient:
                    DelayedInitialization.Traces.AddGrpcClient(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.SqlClient:
                    DelayedInitialization.Traces.AddSqlClient(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.Wcf:
                    DelayedInitialization.Traces.AddWcf(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.Quartz:
                    DelayedInitialization.Traces.AddQuartz(lazyInstrumentationLoader, pluginManager);
                    break;
#if NET6_0_OR_GREATER
                case TracerInstrumentation.AspNetCore:
                    DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.MySqlData:
                    DelayedInitialization.Traces.AddMySqlClient(LazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.EntityFrameworkCore:
                    DelayedInitialization.Traces.AddEntityFrameworkCore(LazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.MongoDB:
                    break;
                case TracerInstrumentation.StackExchangeRedis:
                    break;
                case TracerInstrumentation.MassTransit:
                    break;
#endif
                case TracerInstrumentation.GraphQL:
                    break;
                case TracerInstrumentation.Npgsql:
                    break;
                case TracerInstrumentation.NServiceBus:
                    break;
                case TracerInstrumentation.Elasticsearch:
                    break;
                default:
                    Logger.Warning($"Configured trace instrumentation type is not supported: {instrumentation}");
                    break;
            }
        }
    }

    private static void OnExit(object? sender, EventArgs e)
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
            _sdkEventListener?.Dispose();

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

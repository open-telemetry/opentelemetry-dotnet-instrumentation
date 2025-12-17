// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
internal static class Instrumentation
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly LazyInstrumentationLoader LazyInstrumentationLoader = new();

    private static readonly Lazy<LoggerProvider?> LoggerProviderFactory = new(InitializeLoggerProvider, true);

    private static int _initialized;
    private static int _isExiting;
    private static SdkSelfDiagnosticsEventListener? _sdkEventListener;

    private static TracerProvider? _tracerProvider;
    private static MeterProvider? _meterProvider;

    private static PluginManager? _pluginManager;

    private static SampleExporter? _sampleExporter;
    private static SampleExporterBuilder? _sampleExporterBuilder;

#if NETFRAMEWORK
    private static CanaryThreadManager? _canaryThreadManager;
#endif

    internal static LoggerProvider? LoggerProvider
    {
        get => LoggerProviderFactory.Value;
    }

    internal static PluginManager? PluginManager => _pluginManager;

    internal static ILifespanManager LifespanManager => LazyInstrumentationLoader.LifespanManager;

    internal static Lazy<FailFastSettings> FailFastSettings { get; } = new(() => Settings.FromDefaultSources<FailFastSettings>(false));

    internal static Lazy<GeneralSettings> GeneralSettings { get; } = new(() => Settings.FromDefaultSources<GeneralSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<ResourceSettings> ResourceSettings { get; } = new(() => Settings.FromDefaultSources<ResourceSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<TracerSettings> TracerSettings { get; } = new(() => Settings.FromDefaultSources<TracerSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<MetricSettings> MetricSettings { get; } = new(() => Settings.FromDefaultSources<MetricSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<LogSettings> LogSettings { get; } = new(() => Settings.FromDefaultSources<LogSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<SdkSettings> SdkSettings { get; } = new(() => Settings.FromDefaultSources<SdkSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<NoCodeSettings> NoCodeSettings { get; } = new(() => Settings.FromDefaultSources<NoCodeSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<PluginsSettings> PluginsSettings { get; } = new(() => Settings.FromDefaultSources<PluginsSettings>(FailFastSettings.Value.FailFast));

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
            // We are doing so as the OTel .NET SDK only supports the env vars and we want to
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
            // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
            _sdkEventListener = new(Logger);

            _pluginManager = new PluginManager(PluginsSettings.Value);
            _pluginManager.Initializing();

            // Register to shutdown events
            AppDomain.CurrentDomain.ProcessExit += OnExit;
            AppDomain.CurrentDomain.DomainUnload += OnExit;

            var profilerEnabled = GeneralSettings.Value.ProfilerEnabled;

            if (profilerEnabled)
            {
                TryInitializeContinuousProfiling();
            }
            else
            {
                Logger.Information("CLR Profiler is not enabled. Continuous Profiler will be not started even if configured correctly.");
            }

            if (TracerSettings.Value.TracesEnabled || MetricSettings.Value.MetricsEnabled)
            {
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
                        .InvokePluginsBefore(_pluginManager)
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(ResourceSettings.Value))
                        .UseEnvironmentVariables(LazyInstrumentationLoader, TracerSettings.Value, _pluginManager)
                        .InvokePluginsAfter(_pluginManager);

                    _tracerProvider = builder.Build();
                    _tracerProvider.TryCallInitialized(_pluginManager);
                    Logger.Information("OpenTelemetry tracer initialized.");
                }
                else
                {
                    AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader, _pluginManager, TracerSettings.Value);
                    Logger.Information("Initialized lazily-loaded trace instrumentations without initializing sdk.");
                }
            }

            if (MetricSettings.Value.MetricsEnabled)
            {
                if (GeneralSettings.Value.SetupSdk)
                {
                    var builder = Sdk
                        .CreateMeterProviderBuilder()
                        .InvokePluginsBefore(_pluginManager)
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(ResourceSettings.Value))
                        .UseEnvironmentVariables(LazyInstrumentationLoader, MetricSettings.Value, _pluginManager)
                        .InvokePluginsAfter(_pluginManager);

                    _meterProvider = builder.Build();
                    _meterProvider.TryCallInitialized(_pluginManager);
                    Logger.Information("OpenTelemetry meter initialized.");
                }
                else
                {
                    AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader, _pluginManager, MetricSettings.Value.EnabledInstrumentations);

                    Logger.Information("Initialized lazily-loaded metric instrumentations without initializing sdk.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTelemetry SDK load exception.");
            throw;
        }

        if (GeneralSettings.Value.ProfilerEnabled)
        {
            RegisterDirectBytecodeInstrumentations(InstrumentationDefinitions.GetAllDefinitions());
            if (NoCodeSettings.Value.Enabled)
            {
                NoCodeIntegrationHelper.NoCodeEntries = NoCodeSettings.Value.InstrumentedMethods;
                RegisterBytecodeInstrumentations(NoCodeSettings.Value.GetDirectPayload(), "direct, no-code", NativeMethods.AddInstrumentations);
            }

            try
            {
                foreach (var payload in _pluginManager.GetAllDefinitionsPayloads())
                {
                    RegisterDirectBytecodeInstrumentations(payload);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception occurred while registering instrumentations from plugins.");
            }

            RegisterBytecodeDerivedInstrumentations(InstrumentationDefinitions.GetDerivedDefinitions());
        }
        else
        {
            Logger.Debug("Skipping CLR Profiler initialization. {0} environment variable was not set to '1'.", ConfigurationKeys.ProfilingEnabled);
        }

        if (TracerSettings.Value.OpenTracingEnabled)
        {
            OpenTracingHelper.EnableOpenTracing(_tracerProvider);
        }
    }

    private static void TryInitializeContinuousProfiling()
    {
        try
        {
            InitializeSampling();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize continuous profiling.");
        }
    }

    private static void InitializeSampling()
    {
        var (threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter) = _pluginManager!.GetFirstContinuousConfiguration();
        Logger.Debug($"Continuous profiling configuration: Thread sampling enabled: {threadSamplingEnabled}, thread sampling interval: {threadSamplingInterval}, allocation sampling enabled: {allocationSamplingEnabled}, max memory samples per minute: {maxMemorySamplesPerMinute}, export interval: {exportInterval}, export timeout: {exportTimeout}, continuous profiler exporter: {continuousProfilerExporter.GetType()}");

        if (threadSamplingEnabled || allocationSamplingEnabled)
        {
            if (!TryInitializeContinuousSamplingExport(continuousProfilerExporter, threadSamplingEnabled, allocationSamplingEnabled, exportInterval, exportTimeout))
            {
                return;
            }
        }

        uint selectiveSamplingInterval = 0;
        var selectiveSamplingConfig = _pluginManager.GetFirstSelectiveSamplingConfiguration();
        if (selectiveSamplingConfig.HasValue)
        {
            var (configuredSamplingInterval, configuredExportInterval, configuredExportTimeout, exporter) = selectiveSamplingConfig.Value;
            if (configuredSamplingInterval == 0 || configuredExportInterval <= TimeSpan.Zero || configuredExportTimeout <= TimeSpan.Zero || exporter == null)
            {
                Logger.Warning("Invalid selective sampling configuration. Feature will not be enabled.");
            }
            else
            {
                Logger.Debug(
                    $"Selective sampling configuration: sampling interval: {configuredSamplingInterval}, export interval: {configuredExportInterval}, export timeout: {configuredExportTimeout}, samples exporter: {exporter.GetType()}");
                selectiveSamplingInterval = configuredSamplingInterval;
                if (!TryInitializeSelectedThreadSamplingExport(exporter, configuredExportInterval, configuredExportTimeout))
                {
                    return;
                }
            }
        }

        if (!threadSamplingEnabled && !allocationSamplingEnabled && !selectiveSamplingConfig.HasValue)
        {
            // No sampling requested.
            return;
        }

        var selectiveSamplingEnabled = selectiveSamplingInterval != 0;

        if (threadSamplingEnabled && selectiveSamplingEnabled)
        {
            if (threadSamplingInterval <= selectiveSamplingInterval)
            {
                Logger.Warning($"Continuous sampling interval must be higher than selective sampling interval. Selective sampling interval: {selectiveSamplingInterval}, continuous sampling interval: {threadSamplingInterval}");
                return;
            }

            if (threadSamplingInterval % selectiveSamplingInterval != 0)
            {
                Logger.Warning($"Continuous sampling interval must be a multiple of selective sampling interval. Selective sampling interval: {selectiveSamplingInterval}, continuous sampling interval: {threadSamplingInterval}");
                return;
            }
        }

        NativeMethods.ConfigureNativeContinuousProfiler(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, selectiveSamplingInterval);
#if NETFRAMEWORK
        // On .NET Framework, we need a dedicated canary thread for seeded stack walking
        _canaryThreadManager = new CanaryThreadManager();
        if (!_canaryThreadManager.Start(TimeSpan.FromSeconds(5)))
        {
            Logger.Error("Failed to start canary thread. Continuous profiling will not be enabled.");
            _canaryThreadManager.Dispose();
            _canaryThreadManager = null;
            return;
        }

        Logger.Information("Canary thread started successfully for .NET Framework profiling.");
#endif
        _sampleExporter = _sampleExporterBuilder?.Build();
    }

    private static bool TryInitializeSelectedThreadSamplingExport(
        object? exporter,
        TimeSpan exportInterval,
        TimeSpan exportTimeout)
    {
        var selectiveSampleExportMethod = exporter?.GetType().GetMethod("ExportSelectedThreadSamples");

        if (selectiveSampleExportMethod == null)
        {
            Logger.Warning("Exporter does not have ExportSelectedThreadSamples method. Selective sampler initialization failed.");
            return false;
        }

        InitializeBufferProcessing(exportInterval, exportTimeout);

#if NET
        var handler = selectiveSampleExportMethod.CreateDelegate<Action<byte[], int, CancellationToken>>(exporter!);
#else
        var handler = (Action<byte[], int, CancellationToken>)selectiveSampleExportMethod.CreateDelegate(typeof(Action<byte[], int, CancellationToken>), exporter!);
#endif
        _sampleExporterBuilder?.AddHandler(SampleType.SelectedThreads, handler, exportTimeout);
        return true;
    }

    private static bool TryInitializeContinuousSamplingExport(
        object continuousProfilerExporter,
        bool threadSamplingEnabled,
        bool allocationSamplingEnabled,
        TimeSpan exportInterval,
        TimeSpan exportTimeout)
    {
        var continuousProfilerExporterType = continuousProfilerExporter.GetType();
        var exportThreadSamplesMethod = continuousProfilerExporterType.GetMethod("ExportThreadSamples");

        if (exportThreadSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportThreadSamples method. Continuous Profiler initialization failed.");
            return false;
        }

        var exportAllocationSamplesMethod = continuousProfilerExporterType.GetMethod("ExportAllocationSamples");
        if (exportAllocationSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportAllocationSamples method. Continuous Profiler initialization failed.");
            return false;
        }

#if NET
        var threadSamplesMethod = exportThreadSamplesMethod.CreateDelegate<Action<byte[], int, CancellationToken>>(continuousProfilerExporter);
        var allocationSamplesMethod = exportAllocationSamplesMethod.CreateDelegate<Action<byte[], int, CancellationToken>>(continuousProfilerExporter);
#else
        var threadSamplesMethod = (Action<byte[], int, CancellationToken>)exportThreadSamplesMethod.CreateDelegate(typeof(Action<byte[], int, CancellationToken>), continuousProfilerExporter);
        var allocationSamplesMethod = (Action<byte[], int, CancellationToken>)exportAllocationSamplesMethod.CreateDelegate(typeof(Action<byte[], int, CancellationToken>), continuousProfilerExporter);
#endif

        InitializeBufferProcessing(exportInterval, exportTimeout);

        if (threadSamplingEnabled)
        {
            _sampleExporterBuilder?.AddHandler(SampleType.Continuous, threadSamplesMethod, exportTimeout);
        }

        if (allocationSamplingEnabled)
        {
            _sampleExporterBuilder?.AddHandler(SampleType.Allocation, allocationSamplesMethod, exportTimeout);
        }

        return true;
    }

    private static void InitializeBufferProcessing(TimeSpan exportInterval, TimeSpan exportTimeout)
    {
        _sampleExporterBuilder ??= new SampleExporterBuilder();

        _sampleExporterBuilder
            .SetExportInterval(exportInterval)
            .SetExportTimeout(exportTimeout);
    }

    private static LoggerProvider? InitializeLoggerProvider()
    {
        // ILogger bridge is initialized using ILogger-specific extension methods in LoggerInitializer class.
        // That extension methods sets up its own LogProvider.

        Logger.Debug($"InitializeLoggerProvider called. LogsEnabled={LogSettings.Value.LogsEnabled}, EnableLog4NetBridge={LogSettings.Value.EnableLog4NetBridge}, EnableNLogBridge={LogSettings.Value.EnableNLogBridge}");
        Logger.Debug($"EnabledInstrumentations: {string.Join(", ", LogSettings.Value.EnabledInstrumentations)}");

        // Initialize logger provider if any bridge is enabled
        var shouldInitialize = LogSettings.Value.LogsEnabled && (
            (LogSettings.Value.EnableLog4NetBridge && LogSettings.Value.EnabledInstrumentations.Contains(LogInstrumentation.Log4Net)) ||
            (LogSettings.Value.EnableNLogBridge && LogSettings.Value.EnabledInstrumentations.Contains(LogInstrumentation.NLog)));

        Logger.Debug($"ShouldInitialize logger provider: {shouldInitialize}");

        if (shouldInitialize)
        {
            // TODO: Replace reflection usage when Logs Api is made public in non-rc builds.
            // Sdk.CreateLoggerProviderBuilder()
            var createLoggerProviderBuilderMethod = typeof(Sdk).GetMethod("CreateLoggerProviderBuilder", BindingFlags.Static | BindingFlags.NonPublic)!;
            var loggerProviderBuilder = createLoggerProviderBuilderMethod.Invoke(null, null) as LoggerProviderBuilder;

            // TODO: plugins support
            var loggerProvider = loggerProviderBuilder!
                .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(ResourceSettings.Value))
                .UseEnvironmentVariables(LazyInstrumentationLoader, LogSettings.Value, _pluginManager!)
                .Build();
            Logger.Information("OpenTelemetry logger provider initialized.");
            return loggerProvider;
        }

        return null;
    }

    private static void RegisterDirectBytecodeInstrumentations(InstrumentationDefinitions.Payload payload)
    {
        RegisterBytecodeInstrumentations(payload, "direct", NativeMethods.AddInstrumentations);
    }

    private static void RegisterBytecodeDerivedInstrumentations(InstrumentationDefinitions.Payload payload)
    {
        RegisterBytecodeInstrumentations(payload, "derived", NativeMethods.AddDerivedInstrumentations);
    }

    private static void RegisterBytecodeInstrumentations(InstrumentationDefinitions.Payload payload, string type, Action<string, NativeCallTargetDefinition[]> register)
    {
        try
        {
            Logger.Debug($"Sending CallTarget {type} integration definitions to native library.");
            register(payload.DefinitionsId, payload.Definitions);
            foreach (var def in payload.Definitions)
            {
                def.Dispose();
            }

            Logger.Information("The profiler has been initialized with {0} {1} definitions for {2}.", payload.Definitions.Length, type, payload.DefinitionsId);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }
    }

    private static void AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, IReadOnlyList<MetricInstrumentation> enabledInstrumentations)
    {
        foreach (var instrumentation in enabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case MetricInstrumentation.AspNet:
                    DelayedInitialization.Metrics.AddAspNet(lazyInstrumentationLoader, pluginManager);
                    break;
#endif
#if NET
                case MetricInstrumentation.AspNetCore:
                    break;
#endif
                case MetricInstrumentation.HttpClient:
                    DelayedInitialization.Metrics.AddHttpClient(lazyInstrumentationLoader);
                    break;
                case MetricInstrumentation.NetRuntime:
                    break;
                case MetricInstrumentation.Process:
                    break;
#if NET
                case MetricInstrumentation.Npgsql:
                    break;
#endif
                case MetricInstrumentation.NServiceBus:
                    break;
                case MetricInstrumentation.SqlClient:
                    DelayedInitialization.Metrics.AddSqlClient(lazyInstrumentationLoader, pluginManager);
                    break;
                default:
                    Logger.Warning($"Configured metric instrumentation type is not supported: {instrumentation}");
                    if (FailFastSettings.Value.FailFast)
                    {
                        throw new NotSupportedException($"Configured metric instrumentation type is not supported: {instrumentation}");
                    }

                    break;
            }
        }
    }

    private static void AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        foreach (var instrumentation in tracerSettings.EnabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case TracerInstrumentation.AspNet:
                    DelayedInitialization.Traces.AddAspNet(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.WcfService:
                    break;
#endif
                case TracerInstrumentation.HttpClient:
                    DelayedInitialization.Traces.AddHttpClient(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.GrpcNetClient:
                    DelayedInitialization.Traces.AddGrpcClient(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.SqlClient:
                    DelayedInitialization.Traces.AddSqlClient(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.Quartz:
                    DelayedInitialization.Traces.AddQuartz(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.WcfClient:
                    break;
#if NET
                case TracerInstrumentation.AspNetCore:
                    DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.MySqlData:
                    break;
                case TracerInstrumentation.EntityFrameworkCore:
                    DelayedInitialization.Traces.AddEntityFrameworkCore(LazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.StackExchangeRedis:
                    break;
                case TracerInstrumentation.MassTransit:
                    break;
                case TracerInstrumentation.GraphQL:
                    DelayedInitialization.Traces.AddGraphQL(LazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
#endif
                case TracerInstrumentation.Azure:
                    break;
                case TracerInstrumentation.MongoDB:
                    break;
                case TracerInstrumentation.Npgsql:
                    break;
                case TracerInstrumentation.NServiceBus:
                    break;
                case TracerInstrumentation.Elasticsearch:
                    break;
                case TracerInstrumentation.ElasticTransport:
                    break;
                case TracerInstrumentation.MySqlConnector:
                    break;
                case TracerInstrumentation.Kafka:
                    break;
                case TracerInstrumentation.OracleMda:
                    break;
                case TracerInstrumentation.RabbitMq:
                    break;
                default:
                    Logger.Warning($"Configured trace instrumentation type is not supported: {instrumentation}");
                    if (FailFastSettings.Value.FailFast)
                    {
                        throw new NotSupportedException($"Configured trace instrumentation type is not supported: {instrumentation}");
                    }

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
            LazyInstrumentationLoader?.Dispose();
            _sampleExporter?.Dispose();

#if NETFRAMEWORK
            _canaryThreadManager?.Dispose();
#endif

            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            if (LoggerProviderFactory.IsValueCreated)
            {
                LoggerProvider?.Dispose();
            }

            _sdkEventListener?.Dispose();

            Logger.Information("OpenTelemetry Automatic Instrumentation exit.");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An error occurred while attempting to exit.");
            }
            catch (Exception)
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
        finally
        {
            OtelLogging.CloseLogger("Managed", Logger);
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
        catch (Exception)
        {
            // Logger was already shutdown.
        }
    }
}

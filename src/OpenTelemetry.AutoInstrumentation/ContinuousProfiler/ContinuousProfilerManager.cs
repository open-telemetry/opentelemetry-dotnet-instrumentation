// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal sealed class ContinuousProfilerManager : IDisposable
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly TimeSpan ReaderThreadStartupTimeout = TimeSpan.FromSeconds(5);

#if NETFRAMEWORK
    private static readonly TimeSpan CanaryStatePollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan CanaryThreadStartupTimeout = TimeSpan.FromSeconds(5);
#endif

    private readonly object _lifecycleLock = new();
    private readonly Func<bool> _isExiting;
    private SampleExporter? _sampleExporter;
    private SampleExporterBuilder? _sampleExporterBuilder;

#if NETFRAMEWORK
    private CanaryThreadManager? _canaryThreadManager;
    private bool _readerConfigurationCompatible = true;
#endif

    public ContinuousProfilerManager(Func<bool> isExiting)
    {
        _isExiting = isExiting;
    }

    public void Initialize(PluginManager pluginManager)
    {
        try
        {
            InitializeSampling(pluginManager);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize continuous profiling.");
        }
    }

    public void Dispose()
    {
        SampleExporter? sampleExporter;
#if NETFRAMEWORK
        CanaryThreadManager? canaryThreadManager;
#endif
        lock (_lifecycleLock)
        {
            sampleExporter = _sampleExporter;
            _sampleExporter = null;
#if NETFRAMEWORK
            canaryThreadManager = _canaryThreadManager;
            _canaryThreadManager = null;
#endif
        }

        sampleExporter?.Dispose();

#if NETFRAMEWORK
        canaryThreadManager?.Dispose();
#endif
    }

    internal static (bool Enabled, bool Prepared) GetEffectiveSamplingConfiguration(
        bool enabled,
        uint samplingInterval,
        TimeSpan exportInterval,
        TimeSpan exportTimeout,
        bool exporterConfigured)
    {
        var prepared =
            samplingInterval != 0 &&
            exportInterval > TimeSpan.Zero &&
            exportTimeout > TimeSpan.Zero &&
            exporterConfigured;

        return (enabled && prepared, prepared);
    }

    internal static (bool Enabled, bool Prepared) GetEffectiveAllocationSamplingConfiguration(
        bool enabled,
        uint maxMemorySamplesPerMinute,
        TimeSpan exportInterval,
        TimeSpan exportTimeout,
        bool exporterConfigured)
    {
#if NET
        return GetEffectiveSamplingConfiguration(
            enabled,
            maxMemorySamplesPerMinute,
            exportInterval,
            exportTimeout,
            exporterConfigured);
#else
        return (false, false);
#endif
    }

    internal static bool TryCreateSeedConfiguration(
        bool threadSamplingEnabled,
        uint threadSamplingInterval,
        bool selectiveSamplingEnabled,
        uint selectiveSamplingInterval,
        bool allocationSamplingEnabled,
        uint maxMemorySamplesPerMinute,
        out RuntimeSamplerConfiguration configuration)
    {
        configuration = new RuntimeSamplerConfiguration(
            threadSamplingEnabled ? threadSamplingInterval : 0,
            selectiveSamplingEnabled ? selectiveSamplingInterval : 0,
            allocationSamplingEnabled ? maxMemorySamplesPerMinute : 0);
        return configuration.IsValid;
    }

    internal static bool ShouldActivateManagedPipeline(RuntimeSamplerApplyResult result, RuntimeSamplerState state)
    {
        return result switch
        {
            RuntimeSamplerApplyResult.Applied => state.Authority == RuntimeSamplerAuthority.Seed,
            RuntimeSamplerApplyResult.IgnoredSeedAlreadyCommitted => state.Authority == RuntimeSamplerAuthority.Seed,
            RuntimeSamplerApplyResult.IgnoredLowerAuthority => state.Authority == RuntimeSamplerAuthority.ControlPlane,
            _ => false
        };
    }

    internal static bool CanExportCommittedConfiguration(
        RuntimeSamplerConfiguration configuration,
        bool threadSamplingPrepared,
        bool selectiveSamplingPrepared,
        bool allocationSamplingPrepared)
    {
        return (configuration.CpuSamplingIntervalMilliseconds == 0 || threadSamplingPrepared) &&
               (configuration.SelectiveThreadSamplingIntervalMilliseconds == 0 || selectiveSamplingPrepared) &&
               (configuration.MaxAllocationSamplesPerMinute == 0 || allocationSamplingPrepared);
    }

#if NETFRAMEWORK
    private static string CreateReaderMutexName()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        return @"Local\OpenTelemetry.AutoInstrumentation.ContinuousProfiler.Reader." +
               process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
#endif

    private void InitializeSampling(PluginManager pluginManager)
    {
        var config = pluginManager.GetFirstContinuousProfilerConfiguration();
        Logger.Debug($"Continuous profiling configuration: Thread sampling enabled: {config.ThreadSamplingEnabled}, thread sampling interval: {config.ThreadSamplingInterval}, allocation sampling enabled: {config.AllocationSamplingEnabled}, max memory samples per minute: {config.MaxMemorySamplesPerMinute}, export interval: {config.ExportInterval}, export timeout: {config.ExportTimeout}, continuous profiler exporter: {config.Exporter?.GetType()}");

        var (threadSamplingEnabled, threadSamplingPrepared) = GetEffectiveSamplingConfiguration(
            config.ThreadSamplingEnabled,
            config.ThreadSamplingInterval,
            config.ExportInterval,
            config.ExportTimeout,
            config.Exporter != null);
        var (allocationSamplingEnabled, allocationSamplingPrepared) = GetEffectiveAllocationSamplingConfiguration(
            config.AllocationSamplingEnabled,
            config.MaxMemorySamplesPerMinute,
            config.ExportInterval,
            config.ExportTimeout,
            config.Exporter != null);

        if (config.ThreadSamplingEnabled && !threadSamplingPrepared)
        {
            Logger.Warning("Invalid continuous profiler thread sampling configuration. Thread sampling will not be enabled.");
        }

        if (config.AllocationSamplingEnabled && !allocationSamplingPrepared)
        {
#if NETFRAMEWORK
            Logger.Warning("Continuous profiler allocation sampling is not supported on .NET Framework. Allocation sampling will not be enabled.");
#else
            Logger.Warning("Invalid continuous profiler allocation sampling configuration. Allocation sampling will not be enabled.");
#endif
        }

        var selectiveSamplingEnabled = false;
        var selectiveSamplingPrepared = false;
        uint selectiveSamplingInterval = 0;
        var selectiveSamplingConfig = pluginManager.GetFirstSelectiveSamplingConfiguration();
        if (selectiveSamplingConfig != null)
        {
            (selectiveSamplingEnabled, selectiveSamplingPrepared) = GetEffectiveSamplingConfiguration(
                true,
                selectiveSamplingConfig.SamplingInterval,
                selectiveSamplingConfig.ExportInterval,
                selectiveSamplingConfig.ExportTimeout,
                selectiveSamplingConfig.Exporter != null);

            if (!selectiveSamplingPrepared)
            {
                Logger.Warning("Invalid selective sampling configuration. Selective sampling will not be enabled.");
            }

            if (selectiveSamplingPrepared)
            {
                Logger.Debug(
                    $"Selective sampling configuration: sampling interval: {selectiveSamplingConfig.SamplingInterval}, export interval: {selectiveSamplingConfig.ExportInterval}, export timeout: {selectiveSamplingConfig.ExportTimeout}, samples exporter: {selectiveSamplingConfig.Exporter!.GetType()}");
                selectiveSamplingInterval = selectiveSamplingConfig.SamplingInterval;
            }
        }

        if (!TryCreateSeedConfiguration(
                threadSamplingEnabled,
                config.ThreadSamplingInterval,
                selectiveSamplingEnabled,
                selectiveSamplingInterval,
                allocationSamplingEnabled,
                config.MaxMemorySamplesPerMinute,
                out var seedConfiguration))
        {
            Logger.Warning($"Continuous profiler configuration is invalid. Selective sampling interval: {selectiveSamplingInterval}, continuous sampling interval: {config.ThreadSamplingInterval}. The complete Seed will not be applied.");
            return;
        }

        if (threadSamplingPrepared || allocationSamplingPrepared)
        {
            if (!TryInitializeContinuousSamplingExport(
                    config.Exporter!,
                    threadSamplingPrepared,
                    allocationSamplingPrepared,
                    config.ExportInterval,
                    config.ExportTimeout))
            {
                return;
            }
        }

        if (selectiveSamplingPrepared && !TryInitializeSelectedThreadSamplingExport(selectiveSamplingConfig!))
        {
            return;
        }

        if (!threadSamplingPrepared && !allocationSamplingPrepared && !selectiveSamplingPrepared)
        {
            // No sampling or export pipeline requested.
            return;
        }

        SampleExporter? sampleExporter = null;
        try
        {
#pragma warning disable CA2000 // Ownership transfers to _sampleExporter; finally disposes unpublished exporters.
            sampleExporter = _sampleExporterBuilder!.Build();
#pragma warning restore CA2000
#if NETFRAMEWORK
            // Apply enables native thread callbacks. Create the canary afterwards, including when a later
            // ControlPlane update enables thread sampling after an all-disabled Seed.
            sampleExporter.Start(
                EnsureNetFrameworkCanaryThread,
                CanaryStatePollingInterval,
                CreateReaderMutexName(),
                () => CanReadCurrentConfiguration(
                    threadSamplingPrepared,
                    selectiveSamplingPrepared,
                    allocationSamplingPrepared));
#else
            sampleExporter.Start();
#endif
            if (!sampleExporter.WaitForReaderThreadStart(ReaderThreadStartupTimeout))
            {
                Logger.Error("Continuous profiler reader thread did not start.");
                return;
            }

            var result = NativeMethods.ApplyContinuousProfilerConfiguration(
                seedConfiguration,
                RuntimeSamplerAuthority.Seed,
                out var state);

            Logger.Information(
                $"Continuous profiler Seed result: {result}. Authoritative state: authority: {state.Authority}, CPU sampling interval: {state.CommittedConfiguration.CpuSamplingIntervalMilliseconds}, selective sampling interval: {state.CommittedConfiguration.SelectiveThreadSamplingIntervalMilliseconds}, max allocation samples per minute: {state.CommittedConfiguration.MaxAllocationSamplesPerMinute}.");

            // Each AppDomain tracks its own Activity context. On .NET Framework, the reader mutex
            // allows one AppDomain at a time to consume the process-wide native buffers.
            if (!ShouldActivateManagedPipeline(result, state))
            {
                return;
            }

#if NET
            if (!CanExportCommittedConfiguration(
                    state.CommittedConfiguration,
                    threadSamplingPrepared,
                    selectiveSamplingPrepared,
                    allocationSamplingPrepared))
            {
                Logger.Warning("No exporter is configured for part of the committed continuous profiler configuration. Samples from those native producers will not be exported.");
            }
#endif

            lock (_lifecycleLock)
            {
                if (_isExiting())
                {
                    return;
                }

                sampleExporter.Activate();
                _sampleExporter = sampleExporter;
                sampleExporter = null;
            }
        }
        finally
        {
            sampleExporter?.Dispose();
        }
    }

#if NETFRAMEWORK
    private bool CanReadCurrentConfiguration(
        bool threadSamplingPrepared,
        bool selectiveSamplingPrepared,
        bool allocationSamplingPrepared)
    {
        if (_isExiting())
        {
            return false;
        }

        var result = NativeMethods.GetContinuousProfilerState(out var state);
        var compatible = result == RuntimeSamplerStateQueryResult.Succeeded &&
                         CanExportCommittedConfiguration(
                             state.CommittedConfiguration,
                             threadSamplingPrepared,
                             selectiveSamplingPrepared,
                             allocationSamplingPrepared);

        if (!compatible && _readerConfigurationCompatible)
        {
            Logger.Warning("This AppDomain cannot read the committed continuous profiler configuration because its state is unavailable or an exporter is missing. Its reader will wait for a compatible configuration.");
        }

        _readerConfigurationCompatible = compatible;
        return compatible;
    }

    private bool EnsureNetFrameworkCanaryThread()
    {
        lock (_lifecycleLock)
        {
            if (_isExiting())
            {
                return false;
            }

            if (_canaryThreadManager != null)
            {
                return true;
            }
        }

        var result = NativeMethods.GetContinuousProfilerState(out var state);
        if (result != RuntimeSamplerStateQueryResult.Succeeded)
        {
            Logger.Warning($"Failed to query the continuous profiler state while waiting to start the .NET Framework canary thread. Result: {result}.");
            return false;
        }

        if (!state.CommittedConfiguration.ThreadSamplingEnabled)
        {
            return false;
        }

        return TryStartNetFrameworkCanaryThread();
    }

    private bool TryStartNetFrameworkCanaryThread()
    {
        CanaryThreadManager? canaryThreadManager = null;
        try
        {
            lock (_lifecycleLock)
            {
                if (_isExiting())
                {
                    return false;
                }

                if (_canaryThreadManager != null)
                {
                    return true;
                }
            }

            canaryThreadManager = new CanaryThreadManager();
            if (!canaryThreadManager.Start(CanaryThreadStartupTimeout))
            {
                Logger.Error("Canary thread did not become ready within the startup timeout.");
                return false;
            }

            lock (_lifecycleLock)
            {
                if (_isExiting())
                {
                    return false;
                }

                if (_canaryThreadManager == null)
                {
                    _canaryThreadManager = canaryThreadManager;
                    canaryThreadManager = null;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize the .NET Framework continuous profiler canary thread.");
            return false;
        }
        finally
        {
            canaryThreadManager?.Dispose();
        }
    }
#endif

    private bool TryInitializeSelectedThreadSamplingExport(SelectiveSamplerConfiguration configuration)
    {
        InitializeBufferProcessing(configuration.ExportTimeout);

        _sampleExporterBuilder?.AddHandler(SampleType.SelectedThreads, configuration.Exporter!.ExportSelectedThreadSamples, configuration.ExportInterval, configuration.ExportTimeout);
        return true;
    }

    private bool TryInitializeContinuousSamplingExport(
        IContinuousProfilerExporter exporter,
        bool threadSamplingPrepared,
        bool allocationSamplingPrepared,
        TimeSpan exportInterval,
        TimeSpan exportTimeout)
    {
        InitializeBufferProcessing(exportTimeout);

        if (threadSamplingPrepared)
        {
            _sampleExporterBuilder?.AddHandler(SampleType.Continuous, exporter.ExportThreadSamples, exportInterval, exportTimeout);
        }

        if (allocationSamplingPrepared)
        {
            _sampleExporterBuilder?.AddHandler(SampleType.Allocation, exporter.ExportAllocationSamples, exportInterval, exportTimeout);
        }

        return true;
    }

    private void InitializeBufferProcessing(TimeSpan exportTimeout)
    {
        _sampleExporterBuilder ??= new SampleExporterBuilder();

        _sampleExporterBuilder.SetExportTimeout(exportTimeout);
    }
}

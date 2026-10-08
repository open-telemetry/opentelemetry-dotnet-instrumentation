// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
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
        bool cpuEnabledInSeed,
        uint threadSamplingInterval,
        bool selectiveEnabledInSeed,
        uint selectiveSamplingInterval,
        bool allocationEnabledInSeed,
        uint maxMemorySamplesPerMinute,
        out RuntimeSamplerConfiguration configuration)
    {
        configuration = new RuntimeSamplerConfiguration(
            cpuEnabledInSeed ? threadSamplingInterval : 0,
            selectiveEnabledInSeed ? selectiveSamplingInterval : 0,
            allocationEnabledInSeed ? maxMemorySamplesPerMinute : 0);
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
        bool cpuExportPrepared,
        bool selectiveExportPrepared,
        bool allocationExportPrepared)
    {
        return (configuration.CpuSamplingIntervalMilliseconds == 0 || cpuExportPrepared) &&
               (configuration.SelectiveThreadSamplingIntervalMilliseconds == 0 || selectiveExportPrepared) &&
               (configuration.MaxAllocationSamplesPerMinute == 0 || allocationExportPrepared);
    }

#if NETFRAMEWORK
    private static string CreateReaderMutexName()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        return @"Local\OpenTelemetry.AutoInstrumentation.ContinuousProfiler.Reader." +
               process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
#endif

    private static SamplingStartupPlan? EvaluateConfiguration(PluginManager pluginManager)
    {
        var config = pluginManager.GetFirstContinuousProfilerConfiguration();
        Logger.Debug($"Continuous profiling configuration: Thread sampling enabled: {config.ThreadSamplingEnabled}, thread sampling interval: {config.ThreadSamplingInterval}, allocation sampling enabled: {config.AllocationSamplingEnabled}, max memory samples per minute: {config.MaxMemorySamplesPerMinute}, export interval: {config.ExportInterval}, export timeout: {config.ExportTimeout}, continuous profiler exporter: {config.Exporter?.GetType()}");

        var (cpuEnabledInSeed, cpuExportPrepared) = GetEffectiveSamplingConfiguration(
            config.ThreadSamplingEnabled,
            config.ThreadSamplingInterval,
            config.ExportInterval,
            config.ExportTimeout,
            config.Exporter != null);
        var (allocationEnabledInSeed, allocationExportPrepared) = GetEffectiveAllocationSamplingConfiguration(
            config.AllocationSamplingEnabled,
            config.MaxMemorySamplesPerMinute,
            config.ExportInterval,
            config.ExportTimeout,
            config.Exporter != null);

        if (config.ThreadSamplingEnabled && !cpuExportPrepared)
        {
            Logger.Warning("Invalid continuous profiler thread sampling configuration. Thread sampling will not be enabled.");
        }

        if (config.AllocationSamplingEnabled && !allocationExportPrepared)
        {
#if NETFRAMEWORK
            Logger.Warning("Continuous profiler allocation sampling is not supported on .NET Framework. Allocation sampling will not be enabled.");
#else
            Logger.Warning("Invalid continuous profiler allocation sampling configuration. Allocation sampling will not be enabled.");
#endif
        }

        var selectiveEnabledInSeed = false;
        var selectiveExportPrepared = false;
        uint selectiveSamplingInterval = 0;
        var selectiveSamplingConfig = pluginManager.GetFirstSelectiveSamplingConfiguration();
        if (selectiveSamplingConfig != null)
        {
            (selectiveEnabledInSeed, selectiveExportPrepared) = GetEffectiveSamplingConfiguration(
                true,
                selectiveSamplingConfig.SamplingInterval,
                selectiveSamplingConfig.ExportInterval,
                selectiveSamplingConfig.ExportTimeout,
                selectiveSamplingConfig.Exporter != null);

            if (!selectiveExportPrepared)
            {
                Logger.Warning("Invalid selective sampling configuration. Selective sampling will not be enabled.");
            }

            if (selectiveExportPrepared)
            {
                Logger.Debug(
                    $"Selective sampling configuration: sampling interval: {selectiveSamplingConfig.SamplingInterval}, export interval: {selectiveSamplingConfig.ExportInterval}, export timeout: {selectiveSamplingConfig.ExportTimeout}, samples exporter: {selectiveSamplingConfig.Exporter!.GetType()}");
                selectiveSamplingInterval = selectiveSamplingConfig.SamplingInterval;
            }
        }

        if (!TryCreateSeedConfiguration(
                cpuEnabledInSeed,
                config.ThreadSamplingInterval,
                selectiveEnabledInSeed,
                selectiveSamplingInterval,
                allocationEnabledInSeed,
                config.MaxMemorySamplesPerMinute,
                out var seedConfiguration))
        {
            Logger.Warning($"Continuous profiler configuration is invalid. Selective sampling interval: {selectiveSamplingInterval}, continuous sampling interval: {config.ThreadSamplingInterval}. The complete Seed will not be applied.");
            return null;
        }

        return new SamplingStartupPlan(
            seedConfiguration,
            cpuExportPrepared,
            selectiveExportPrepared,
            allocationExportPrepared,
            config,
            selectiveSamplingConfig);
    }

    private static void AddSelectiveHandler(SampleExporterBuilder builder, SamplingStartupPlan plan)
    {
        builder.SetExportTimeout(plan.SelectiveExportTimeout);
        builder.AddHandler(SampleType.SelectedThreads, plan.SelectiveExporter!.ExportSelectedThreadSamples, plan.SelectiveExportInterval, plan.SelectiveExportTimeout);
    }

    private static void AddContinuousHandlers(SampleExporterBuilder builder, SamplingStartupPlan plan)
    {
        builder.SetExportTimeout(plan.ContinuousExportTimeout);
        var exporter = plan.ContinuousExporter!;

        if (plan.CpuExportPrepared)
        {
            builder.AddHandler(SampleType.Continuous, exporter.ExportThreadSamples, plan.ContinuousExportInterval, plan.ContinuousExportTimeout);
        }

        if (plan.AllocationExportPrepared)
        {
            builder.AddHandler(SampleType.Allocation, exporter.ExportAllocationSamples, plan.ContinuousExportInterval, plan.ContinuousExportTimeout);
        }
    }

    private void InitializeSampling(PluginManager pluginManager)
    {
        var plan = EvaluateConfiguration(pluginManager);
        if (plan == null || !plan.HasPreparedExports)
        {
            return;
        }

        var builder = new SampleExporterBuilder();
        if (plan.CpuExportPrepared || plan.AllocationExportPrepared)
        {
            AddContinuousHandlers(builder, plan);
        }

        if (plan.SelectiveExportPrepared)
        {
            AddSelectiveHandler(builder, plan);
        }

        SampleExporter? candidateExporter = null;
        try
        {
#pragma warning disable CA2000 // Ownership transfers to _sampleExporter; finally disposes unpublished exporters.
            candidateExporter = builder.Build();
#pragma warning restore CA2000
#if NETFRAMEWORK
            // Apply enables native thread callbacks. Create the canary afterwards, including when a later
            // ControlPlane update enables thread sampling after an all-disabled Seed.
            candidateExporter.Start(
                EnsureNetFrameworkCanaryThread,
                CanaryStatePollingInterval,
                CreateReaderMutexName(),
                () => CanReadCurrentConfiguration(plan));
#else
            candidateExporter.Start();
#endif
            if (!candidateExporter.WaitForReaderThreadStart(ReaderThreadStartupTimeout))
            {
                Logger.Error("Continuous profiler reader thread did not start.");
                return;
            }

            var result = NativeMethods.ApplyContinuousProfilerConfiguration(
                plan.Seed,
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
            if (!plan.CanExport(state.CommittedConfiguration))
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

                candidateExporter.Activate();
                _sampleExporter = candidateExporter;
                // Clearing the candidate marks the transfer of ownership to this manager.
                candidateExporter = null;
            }
        }
        finally
        {
            candidateExporter?.Dispose();
        }
    }

#if NETFRAMEWORK
    private bool CanReadCurrentConfiguration(SamplingStartupPlan plan)
    {
        if (_isExiting())
        {
            return false;
        }

        var result = NativeMethods.GetContinuousProfilerState(out var state);
        var compatible = result == RuntimeSamplerStateQueryResult.Succeeded &&
                         plan.CanExport(state.CommittedConfiguration);

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
}

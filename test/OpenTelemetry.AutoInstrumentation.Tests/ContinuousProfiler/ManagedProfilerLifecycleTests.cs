// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using NSubstitute;
using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

namespace OpenTelemetry.AutoInstrumentation.Tests.ContinuousProfiler;

public class ManagedProfilerLifecycleTests
{
    [Fact]
    public void ValidDisabledConfigurationPreparesPipeline()
    {
        var configuration = CreateContinuousConfiguration();
        var plan = ContinuousProfilerManager.EvaluateConfiguration(configuration, null);

        Assert.NotNull(plan);
        Assert.False(plan.Seed.AnyFeatureEnabled);
        Assert.True(plan.CpuExportPrepared);
        Assert.True(plan.HasPreparedExports);
    }

    [Fact]
    public void AllocationSamplingPreparationIsPlatformAware()
    {
        var configuration = CreateContinuousConfiguration();
        configuration.ThreadSamplingInterval = 0;
        configuration.AllocationSamplingEnabled = true;
        configuration.MaxMemorySamplesPerMinute = 100;
        var plan = ContinuousProfilerManager.EvaluateConfiguration(configuration, null);

        Assert.NotNull(plan);

#if NET
        Assert.Equal(100u, plan.Seed.MaxAllocationSamplesPerMinute);
        Assert.True(plan.AllocationExportPrepared);
        Assert.True(plan.HasPreparedExports);
#else
        Assert.Equal(0u, plan.Seed.MaxAllocationSamplesPerMinute);
        Assert.False(plan.AllocationExportPrepared);
        Assert.False(plan.HasPreparedExports);
#endif
    }

    [Theory]
    [InlineData(0, 1, 1, true)]
    [InlineData(1, 0, 1, true)]
    [InlineData(1, 1, 0, true)]
    [InlineData(1, 1, 1, false)]
    public void InvalidConfigurationDoesNotPreparePipeline(
        uint samplingInterval,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds,
        bool exporterConfigured)
    {
        var configuration = CreateContinuousConfiguration();
        configuration.ThreadSamplingEnabled = true;
        configuration.ThreadSamplingInterval = samplingInterval;
        configuration.ExportInterval = TimeSpan.FromMilliseconds(exportIntervalMilliseconds);
        configuration.ExportTimeout = TimeSpan.FromMilliseconds(exportTimeoutMilliseconds);
        if (!exporterConfigured)
        {
            configuration.Exporter = null;
        }

        var plan = ContinuousProfilerManager.EvaluateConfiguration(configuration, null);

        Assert.NotNull(plan);
        Assert.False(plan.Seed.AnyFeatureEnabled);
        Assert.False(plan.CpuExportPrepared);
        Assert.False(plan.HasPreparedExports);
    }

    [Fact]
    public void InvalidCompleteSeedIsRejectedWithoutPartialNormalization()
    {
        const uint cpuInterval = 1000;
        const uint selectiveInterval = 300;
        var accepted = ContinuousProfilerManager.TryCreateSeedConfiguration(
            true,
            cpuInterval,
            true,
            selectiveInterval,
            false,
            0,
            out var configuration);

        Assert.False(accepted);
        Assert.Equal(cpuInterval, configuration.CpuSamplingIntervalMilliseconds);
        Assert.Equal(selectiveInterval, configuration.SelectiveThreadSamplingIntervalMilliseconds);
    }

    [Theory]
    [InlineData((int)RuntimeSamplerApplyResult.Applied, (uint)RuntimeSamplerAuthority.Seed, true)]
    [InlineData((int)RuntimeSamplerApplyResult.IgnoredSeedAlreadyCommitted, (uint)RuntimeSamplerAuthority.Seed, true)]
    [InlineData((int)RuntimeSamplerApplyResult.IgnoredLowerAuthority, (uint)RuntimeSamplerAuthority.ControlPlane, true)]
    [InlineData((int)RuntimeSamplerApplyResult.Applied, (uint)RuntimeSamplerAuthority.ControlPlane, false)]
    [InlineData((int)RuntimeSamplerApplyResult.NoChange, (uint)RuntimeSamplerAuthority.Seed, false)]
    [InlineData((int)RuntimeSamplerApplyResult.RejectedInvalidConfiguration, (uint)RuntimeSamplerAuthority.Seed, false)]
    [InlineData((int)RuntimeSamplerApplyResult.IgnoredSeedAlreadyCommitted, (uint)RuntimeSamplerAuthority.None, false)]
    public void ManagedPipelineActivatesOnlyForCommittedConfiguration(
        int result,
        uint authority,
        bool expected)
    {
        var state = RuntimeSamplerState.Create();
        state.Authority = (RuntimeSamplerAuthority)authority;

        Assert.Equal(expected, ContinuousProfilerManager.ShouldActivateManagedPipeline((RuntimeSamplerApplyResult)result, state));
    }

    [Fact]
    public void ReaderRequiresExportersForEveryCommittedProducer()
    {
        var committedConfiguration = new RuntimeSamplerConfiguration(500, 100, 200);

        Assert.False(ContinuousProfilerManager.CanExportCommittedConfiguration(committedConfiguration, true, false, true));
        Assert.False(ContinuousProfilerManager.CanExportCommittedConfiguration(committedConfiguration, true, true, false));
        Assert.False(ContinuousProfilerManager.CanExportCommittedConfiguration(committedConfiguration, false, true, true));
        Assert.True(ContinuousProfilerManager.CanExportCommittedConfiguration(committedConfiguration, true, true, true));
    }

#if NETFRAMEWORK
    [Fact]
    public void ReaderTransfersWhenCommittedConfigurationChanges()
    {
        var mutexName = $"OpenTelemetry.AutoInstrumentation.Tests.Reader.{Guid.NewGuid():N}";
        var firstReadEventName = $"{mutexName}.FirstRead";
        var secondReadEventName = $"{mutexName}.SecondRead";
        var firstEligibilityEventName = $"{mutexName}.FirstEligible";
        var secondEligibilityEventName = $"{mutexName}.SecondEligible";
        using var firstRead = new EventWaitHandle(false, EventResetMode.ManualReset, firstReadEventName);
        using var secondRead = new EventWaitHandle(false, EventResetMode.ManualReset, secondReadEventName);
        using var firstEligible = new EventWaitHandle(true, EventResetMode.ManualReset, firstEligibilityEventName);
        using var secondEligible = new EventWaitHandle(false, EventResetMode.ManualReset, secondEligibilityEventName);
        var domainSetup = new AppDomainSetup
        {
            ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
        };
        AppDomain? firstDomain = null;
        AppDomain? secondDomain = null;

        try
        {
            secondDomain = AppDomain.CreateDomain("Continuous profiler second reader", null, domainSetup);
            var second = CreateExporterInDomain(secondDomain);
            second.Start(mutexName, secondReadEventName, secondEligibilityEventName);
            Assert.False(secondRead.WaitOne(TimeSpan.FromMilliseconds(100)));

            firstDomain = AppDomain.CreateDomain("Continuous profiler first reader", null, domainSetup);
            var first = CreateExporterInDomain(firstDomain);
            first.Start(mutexName, firstReadEventName, firstEligibilityEventName);
            Assert.True(firstRead.WaitOne(TimeSpan.FromSeconds(2)));

            firstEligible.Reset();
            secondEligible.Set();

            Assert.True(secondRead.WaitOne(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            if (firstDomain != null)
            {
                AppDomain.Unload(firstDomain);
            }

            if (secondDomain != null)
            {
                AppDomain.Unload(secondDomain);
            }
        }
    }

    [Fact]
    public void ReaderMovesToAnotherAppDomainAfterOwnerUnload()
    {
        var mutexName = $"OpenTelemetry.AutoInstrumentation.Tests.Reader.{Guid.NewGuid():N}";
        var firstReadEventName = $"{mutexName}.FirstRead";
        var secondReadEventName = $"{mutexName}.SecondRead";
        using var firstRead = new EventWaitHandle(false, EventResetMode.ManualReset, firstReadEventName);
        using var secondRead = new EventWaitHandle(false, EventResetMode.ManualReset, secondReadEventName);
        var domainSetup = new AppDomainSetup
        {
            ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
        };
        AppDomain? firstDomain = null;
        AppDomain? secondDomain = null;

        try
        {
            firstDomain = AppDomain.CreateDomain("Continuous profiler first reader", null, domainSetup);
            secondDomain = AppDomain.CreateDomain("Continuous profiler second reader", null, domainSetup);
            var first = CreateExporterInDomain(firstDomain);
            first.Start(mutexName, firstReadEventName);
            Assert.True(firstRead.WaitOne(TimeSpan.FromSeconds(2)));

            var second = CreateExporterInDomain(secondDomain);
            second.Start(mutexName, secondReadEventName);
            Assert.False(secondRead.WaitOne(TimeSpan.FromMilliseconds(250)));

            AppDomain.Unload(firstDomain);
            firstDomain = null;

            Assert.True(secondRead.WaitOne(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            if (firstDomain != null)
            {
                AppDomain.Unload(firstDomain);
            }

            if (secondDomain != null)
            {
                AppDomain.Unload(secondDomain);
            }
        }
    }
#endif

    [Fact]
    public void SampleExporterReadsOnlyAfterActivation()
    {
        using var readAttempted = new ManualResetEventSlim();
        var processor = CreateBufferProcessor(readAttempted);
        using var exporter = new SampleExporter(processor, TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1));

        exporter.Start();
        Assert.True(exporter.WaitForReaderThreadStart(TimeSpan.FromSeconds(2)));
        Assert.False(readAttempted.IsSet);
        exporter.Activate();

        Assert.True(readAttempted.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void SampleExporterStopsBeforeActivation()
    {
        using var readAttempted = new ManualResetEventSlim();
        var processor = CreateBufferProcessor(readAttempted);
        using var exporter = new SampleExporter(processor, TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1));

        exporter.Start();
        Assert.True(exporter.WaitForReaderThreadStart(TimeSpan.FromSeconds(2)));
        exporter.Dispose();

        Assert.False(readAttempted.IsSet);
    }

    [Fact]
    public void SampleExporterRetriesDeferredInitializerBeforeExportInterval()
    {
        using var readAttempted = new ManualResetEventSlim();
        using var initialized = new ManualResetEventSlim();
        var processor = CreateBufferProcessor(readAttempted);
        using var exporter = new SampleExporter(processor, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        var attempts = 0;

        exporter.Start(
            () =>
            {
                if (Interlocked.Increment(ref attempts) < 2)
                {
                    return false;
                }

                initialized.Set();
                return true;
            },
            TimeSpan.FromMilliseconds(10));
        exporter.Activate();

        Assert.True(initialized.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(2, Volatile.Read(ref attempts));
        Assert.False(readAttempted.IsSet);
    }

    [Fact]
    public void PreparedHandlerDoesNotDelayActiveExporter()
    {
        using var activeExported = new ManualResetEventSlim();
        var preparedReadCount = 0;
        var timeout = TimeSpan.FromSeconds(1);
        var builder = new SampleExporterBuilder()
            .SetExportTimeout(timeout)
            .AddHandler(SampleType.Continuous, (_, _, _, _) => { }, TimeSpan.FromSeconds(5), timeout)
            .AddHandler(SampleType.SelectedThreads, (_, _, _) => activeExported.Set(), TimeSpan.FromMilliseconds(20), timeout);
        using var exporter = builder.Build((sampleType, _) =>
        {
            if (sampleType == SampleType.Continuous)
            {
                Interlocked.Increment(ref preparedReadCount);
                return (0, 0u);
            }

            return (1, 0u);
        });

        exporter.Start();
        exporter.Activate();

        Assert.True(activeExported.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(0, Volatile.Read(ref preparedReadCount));
    }

    private static ContinuousProfilerConfiguration CreateContinuousConfiguration()
    {
        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingInterval = 100,
            ExportInterval = TimeSpan.FromSeconds(1),
            ExportTimeout = TimeSpan.FromSeconds(1),
            Exporter = Substitute.For<IContinuousProfilerExporter>()
        };
    }

    private static BufferProcessor CreateBufferProcessor(ManualResetEventSlim readAttempted)
    {
        var handlers = new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, _, _) => { }, TimeSpan.FromSeconds(1))
        };

        return new BufferProcessor(
            handlers,
            (_, _) =>
            {
                readAttempted.Set();
                return (0, 0u);
            });
    }

#if NETFRAMEWORK
    private static CrossAppDomainSampleExporter CreateExporterInDomain(AppDomain domain)
    {
        return (CrossAppDomainSampleExporter)domain.CreateInstanceAndUnwrap(
            typeof(CrossAppDomainSampleExporter).Assembly.FullName,
            typeof(CrossAppDomainSampleExporter).FullName);
    }
#endif
}

#if NETFRAMEWORK
public sealed class CrossAppDomainSampleExporter : MarshalByRefObject, IDisposable
{
    private SampleExporter? _exporter;
    private EventWaitHandle? _readEvent;
    private EventWaitHandle? _eligibilityEvent;

    public void Start(string mutexName, string readEventName, string? eligibilityEventName = null)
    {
        var readEvent = EventWaitHandle.OpenExisting(readEventName);
        _readEvent = readEvent;
        _eligibilityEvent = eligibilityEventName == null ? null : EventWaitHandle.OpenExisting(eligibilityEventName);
        var handlers = new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, _, _) => { }, TimeSpan.FromSeconds(1))
        };

        var processor = new BufferProcessor(
            handlers,
            (_, _) =>
            {
                readEvent.Set();
                return (0, 0u);
            });

        _exporter = new SampleExporter(processor, TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1));
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        _exporter.Start(() => true, TimeSpan.FromMilliseconds(10), mutexName, () => _eligibilityEvent?.WaitOne(0) ?? true);
        if (!_exporter.WaitForReaderThreadStart(TimeSpan.FromSeconds(2)))
        {
            throw new InvalidOperationException("Continuous profiler reader thread did not start.");
        }

        _exporter.Activate();
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
        _exporter?.Dispose();
        _exporter = null;
        _readEvent?.Dispose();
        _readEvent = null;
        _eligibilityEvent?.Dispose();
        _eligibilityEvent = null;
    }

    private void OnDomainUnload(object sender, EventArgs e) => Dispose();
}
#endif

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "runtime_sampler_service.h"

#include "continuous_profiler.h"
#include "logger.h"
#include "runtime_sampler_configuration.h"
#include "stack_walker_impl.h"

namespace continuous_profiler
{

RuntimeSamplerService::RuntimeSamplerService(ICorProfilerInfo7*  info7,
                                             ICorProfilerInfo12* info12,
                                             const RuntimeType   runtime)
    : info7_(info7), info12_(info12), runtime_(runtime)
{
}

RuntimeSamplerApplyOutcome RuntimeSamplerService::ApplyConfigurationV1(
    const RuntimeSamplerAuthority source, const RuntimeSamplerConfigurationV1& configuration)
{
    std::lock_guard<std::mutex> lock(configurationMutex_);

    const auto outcome = [this](const RuntimeSamplerApplyResult result) {
        return RuntimeSamplerApplyOutcome{result, {authority_, committedConfiguration_}};
    };

    if (shutdownRequested_.load(std::memory_order_acquire))
    {
        return outcome(RuntimeSamplerApplyResult::ShuttingDown);
    }

    if (source != RuntimeSamplerAuthority::Seed && source != RuntimeSamplerAuthority::ControlPlane)
    {
        return outcome(RuntimeSamplerApplyResult::RejectedInvalidArgument);
    }

    if (!configuration.IsValid())
    {
        return outcome(RuntimeSamplerApplyResult::RejectedInvalidConfiguration);
    }

    if (source == RuntimeSamplerAuthority::Seed && authority_ == RuntimeSamplerAuthority::Seed)
    {
        return outcome(RuntimeSamplerApplyResult::IgnoredSeedAlreadyCommitted);
    }

    if (source == RuntimeSamplerAuthority::Seed && authority_ == RuntimeSamplerAuthority::ControlPlane)
    {
        return outcome(RuntimeSamplerApplyResult::IgnoredLowerAuthority);
    }

    if (source == authority_ && configuration == committedConfiguration_)
    {
        return outcome(RuntimeSamplerApplyResult::NoChange);
    }

    const bool authorityOnlyChange =
        authority_ != RuntimeSamplerAuthority::None && configuration == committedConfiguration_;
    if (!authorityOnlyChange && configuration.AnyFeatureEnabled())
    {
        const auto cpuSamplingInterval       = configuration.CpuSamplingInterval();
        const auto selectiveSamplingInterval = configuration.SelectiveThreadSamplingInterval();
        trace::Logger::Info("ConfigureContinuousProfiler: thread sampling enabled: ", configuration.CpuEnabled(),
                            ", thread sampling interval: ", cpuSamplingInterval.count(),
                            ", allocationSamplingEnabled: ", configuration.AllocationEnabled(),
                            ", max memory samples per minute: ", configuration.MaxAllocationSamplesPerMinute(),
                            ", selected threads sampling interval: ", selectiveSamplingInterval.count());

        // Bootstrap the sampler machinery before publishing configuration. The sampling thread starts with the
        // default disabled configuration and therefore remains quiescent until PublishCommittedConfiguration()
        // stages the committed intervals and notifies it below.
        if (!EnsureSamplingInfrastructurePrepared() ||
            (configuration.SelectiveEnabled() && !EnsureSelectiveSamplingBuffersPrepared()) ||
            (configuration.ThreadSamplingEnabled() && !EnsureThreadSamplingStarted()) ||
            (configuration.AllocationEnabled() && !committedConfiguration_.AllocationEnabled() &&
             !EnsureAllocationSamplingStarted()))
        {
            return outcome(RuntimeSamplerApplyResult::ActivationFailed);
        }
    }

    if (shutdownRequested_.load(std::memory_order_acquire))
    {
        return outcome(RuntimeSamplerApplyResult::ShuttingDown);
    }

    const auto previousConfiguration = committedConfiguration_;
    // A successful bootstrap guarantees a sampler. A null sampler is valid only when this commit has no sampler state
    // to publish, such as the initial all-disabled configuration or an authority-only change.
    const bool publishToSampler = !authorityOnlyChange && sampler_ != nullptr;
    if (publishToSampler && previousConfiguration.AllocationEnabled() && !configuration.AllocationEnabled())
    {
        sampler_->UpdateAllocationSamplingTarget(0);
    }

    authority_              = source;
    committedConfiguration_ = configuration;

    if (publishToSampler)
    {
        // Publication is the activation boundary: the sampler observes only committed controller state.
        PublishCommittedConfiguration(previousConfiguration, configuration);
    }
    return outcome(RuntimeSamplerApplyResult::Applied);
}

RuntimeSamplerState RuntimeSamplerService::GetState() const
{
    std::lock_guard<std::mutex> lock(configurationMutex_);
    return {authority_, committedConfiguration_};
}

bool RuntimeSamplerService::HasSamplingInfrastructure() const
{
    std::lock_guard<std::mutex> lock(configurationMutex_);
    return sampler_ != nullptr;
}

RuntimeSamplerService::~RuntimeSamplerService()
{
    Shutdown();
}

bool RuntimeSamplerService::EnsureSamplingInfrastructurePrepared() noexcept
{
    if (samplingInfrastructurePrepared_)
    {
        return true;
    }

    if (info7_ == nullptr)
    {
        return false;
    }

    try
    {
        // These process-lifetime objects are callback targets. Publish them before enabling CLR callbacks so every
        // eligible callback observes a complete and stable sampling infrastructure.
        if (sampler_ == nullptr)
        {
            auto allocationSamplingSessionProvider = std::make_unique<ClrAllocationSamplingSessionProvider>(info12_);
            auto sampler = std::make_unique<ContinuousProfiler>(*allocationSamplingSessionProvider);
            if (info12_ != nullptr)
            {
                sampler->SetGlobalInfo12(info12_);
            }
            else
            {
                sampler->SetGlobalInfo7(info7_);
            }
            allocationSamplingSessionProvider_ = std::move(allocationSamplingSessionProvider);
            sampler_                           = std::move(sampler);
        }

        if (stackWalker_ == nullptr)
        {
            stackWalker_ = std::make_unique<StackWalkerImpl>(info7_, runtime_);
            sampler_->SetStackWalker(stackWalker_.get());
        }

#if defined(_WIN32)
        // .NET Framework may not report ThreadAssignedToOSThread for the managed thread that performs startup.
        ThreadID currentThreadId = 0;
        if (const auto hr = info7_->GetCurrentThreadID(&currentThreadId); SUCCEEDED(hr))
        {
            OnThreadAssignedToOSThread(currentThreadId, ::GetCurrentThreadId());
        }
#endif

        DWORD eventsLow;
        DWORD eventsHigh;
        auto  hr = info7_->GetEventMask2(&eventsLow, &eventsHigh);
        if (FAILED(hr))
        {
            trace::Logger::Warn("RuntimeSamplerService: Failed to read CLR event masks for continuous profiler.");
            return false;
        }

        // Keep both capabilities latched after first enablement. Stack snapshots are passive until requested, while
        // thread callbacks keep thread metadata and .NET Framework canary state coherent across quiescent periods.
        eventsLow |= COR_PRF_MONITOR_THREADS | COR_PRF_ENABLE_STACK_SNAPSHOT;
        hr = info7_->SetEventMask2(eventsLow, eventsHigh);
        if (FAILED(hr))
        {
            trace::Logger::Warn("RuntimeSamplerService: Failed to enable CLR event masks for continuous profiler.");
            return false;
        }

        samplingInfrastructurePrepared_ = true;
        trace::Logger::Info("RuntimeSamplerService: CLR event masks configured for continuous profiler.");
        return true;
    }
    catch (...)
    {
        return false;
    }
}

bool RuntimeSamplerService::EnsureSelectiveSamplingBuffersPrepared() noexcept
{
    if (selectiveSamplingBuffersPrepared_)
    {
        return true;
    }

    try
    {
        ContinuousProfiler::PrepareSelectiveSamplingBuffers();
        selectiveSamplingBuffersPrepared_ = true;
        return true;
    }
    catch (...)
    {
        return false;
    }
}

bool RuntimeSamplerService::EnsureThreadSamplingStarted() noexcept
{
    if (threadSamplingStarted_)
    {
        return true;
    }

    trace::Logger::Info("ContinuousProfiler::StartThreadSampling");
    if (!sampler_->StartThreadSampling())
    {
        return false;
    }

    threadSamplingStarted_ = true;
    return true;
}

bool RuntimeSamplerService::EnsureAllocationSamplingStarted() noexcept
{
    return sampler_->StartAllocationSampling();
}

void RuntimeSamplerService::PublishCommittedConfiguration(const RuntimeSamplerConfigurationV1& previousConfiguration,
                                                          const RuntimeSamplerConfigurationV1& configuration) noexcept
{
    const bool threadConfigurationChanged =
        previousConfiguration.cpuSamplingIntervalMilliseconds != configuration.cpuSamplingIntervalMilliseconds ||
        previousConfiguration.selectiveThreadSamplingIntervalMilliseconds !=
            configuration.selectiveThreadSamplingIntervalMilliseconds;
    if (threadConfigurationChanged)
    {
        sampler_->StageThreadSamplingConfiguration(static_cast<unsigned int>(
                                                       configuration.CpuSamplingInterval().count()),
                                                   static_cast<unsigned int>(
                                                       configuration.SelectiveThreadSamplingInterval().count()));
    }

    if (configuration.AllocationEnabled())
    {
        sampler_->UpdateAllocationSamplingTarget(configuration.MaxAllocationSamplesPerMinute());
    }
    else if (previousConfiguration.AllocationEnabled())
    {
        sampler_->StopAllocationSampling();
    }
}

void RuntimeSamplerService::Shutdown() noexcept
{
    ContinuousProfiler* sampler = nullptr;
    {
        std::lock_guard<std::mutex> lock(configurationMutex_);
        if (shutdownRequested_.exchange(true, std::memory_order_acq_rel))
        {
            return;
        }
        sampler = sampler_.get();
    }

    if (sampler != nullptr)
    {
        sampler->Shutdown();
    }
}

void RuntimeSamplerService::OnThreadCreated(const ThreadID threadId) noexcept
{
    if (shutdownRequested_.load(std::memory_order_acquire) || sampler_ == nullptr)
    {
        return;
    }
    sampler_->ThreadCreated(threadId);
    if (stackWalker_ != nullptr)
    {
        stackWalker_->OnThreadCreated(threadId);
    }
}

void RuntimeSamplerService::OnThreadDestroyed(const ThreadID threadId) noexcept
{
    if (shutdownRequested_.load(std::memory_order_acquire) || sampler_ == nullptr)
    {
        return;
    }
    sampler_->ThreadDestroyed(threadId);
    if (stackWalker_ != nullptr)
    {
        stackWalker_->OnThreadDestroyed(threadId);
    }
}

void RuntimeSamplerService::OnThreadNameChanged(const ThreadID threadId, const ULONG cchName, WCHAR name[]) noexcept
{
    if (shutdownRequested_.load(std::memory_order_acquire) || sampler_ == nullptr)
    {
        return;
    }
    sampler_->ThreadNameChanged(threadId, cchName, name);
    if (stackWalker_ != nullptr)
    {
        stackWalker_->OnThreadNameChanged(threadId, cchName, name);
    }
}

void RuntimeSamplerService::OnThreadAssignedToOSThread(const ThreadID managedThreadId, const DWORD osThreadId) noexcept
{
    if (shutdownRequested_.load(std::memory_order_acquire) || sampler_ == nullptr)
    {
        return;
    }
    if (stackWalker_ != nullptr)
    {
        stackWalker_->OnThreadAssignedToOSThread(managedThreadId, osThreadId);
    }
}

void RuntimeSamplerService::OnAllocationTick(const ULONG dataLength, const LPCBYTE data) noexcept
{
    if (!shutdownRequested_.load(std::memory_order_acquire) && sampler_ != nullptr)
    {
        sampler_->AllocationTick(dataLength, data);
    }
}

} // namespace continuous_profiler

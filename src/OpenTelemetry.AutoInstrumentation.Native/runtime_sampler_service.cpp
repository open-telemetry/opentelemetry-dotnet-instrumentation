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

    if (configuration == committedConfiguration_)
    {
        if (source == authority_)
    {
        return outcome(RuntimeSamplerApplyResult::NoChange);
    }

        authority_ = source;
        return outcome(RuntimeSamplerApplyResult::Applied);
    }

    auto* sampler = sampler_.get();
    if (configuration.AnyFeatureEnabled())
    {
        // Bootstrap creates the sampler in a disabled state. Configuration is published only after every required
        // fallible activation step succeeds. Objects are created before CLR events are enabled so callbacks can only
        // observe complete, stable targets.
        sampler = EnsureSamplerCreated();
        if (sampler == nullptr || !EnsureRequiredClrEventsEnabled())
    {
            return outcome(RuntimeSamplerApplyResult::ActivationFailed);
        }

        if (configuration.SelectiveEnabled() && !EnsureSelectiveSamplingBuffersPrepared())
        {
            trace::Logger::Warn("RuntimeSamplerService: failed to prepare selective-sampling buffers.");
            return outcome(RuntimeSamplerApplyResult::ActivationFailed);
        }

        if (configuration.ThreadSamplingEnabled() && !sampler->StartThreadSampling())
        {
            trace::Logger::Warn("RuntimeSamplerService: failed to start the thread-sampling worker.");
            return outcome(RuntimeSamplerApplyResult::ActivationFailed);
    }

        if (configuration.AllocationEnabled() && !committedConfiguration_.AllocationEnabled() &&
            !sampler->StartAllocationSamplingSession())
    {
            trace::Logger::Warn("RuntimeSamplerService: failed to start the allocation-sampling EventPipe session.");
            return outcome(RuntimeSamplerApplyResult::ActivationFailed);
        }
    }

    const auto previousConfiguration = committedConfiguration_;
    if (sampler != nullptr && !configuration.AllocationEnabled())
    {
        // Close admission before publishing the disabled controller state. EventPipe cleanup follows the commit.
        sampler->UpdateAllocationSamplingTarget(0);
    }

    authority_              = source;
    committedConfiguration_ = configuration;

    if (sampler != nullptr)
    {
        // Publication is the activation boundary: the sampler observes only committed controller state.
        PublishCommittedConfiguration(previousConfiguration, configuration);

        if (!configuration.AllocationEnabled())
        {
            // Cleanup is best effort. A failed stop retains the session handle with admission closed; any later
            // non-identical committed configuration retries the stop, while an identical snapshot remains NoChange.
            (void)sampler->StopAllocationSamplingSession();
    }
    }

    trace::Logger::Info("RuntimeSamplerService: configuration applied. CPU sampling enabled: ",
                        configuration.CpuEnabled(), ", CPU interval: ", configuration.CpuSamplingInterval().count(),
                        " ms, selective sampling enabled: ", configuration.SelectiveEnabled(),
                        ", selective interval: ", configuration.SelectiveThreadSamplingInterval().count(),
                        " ms, allocation sampling enabled: ", configuration.AllocationEnabled(),
                        ", maximum allocation samples per minute: ", configuration.MaxAllocationSamplesPerMinute());
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

ContinuousProfiler* RuntimeSamplerService::EnsureSamplerCreated() noexcept
{
    if (sampler_ != nullptr)
    {
        return sampler_.get();
    }

    if (info7_ == nullptr)
    {
        trace::Logger::Warn("RuntimeSamplerService: ICorProfilerInfo7 is unavailable; sampler creation failed.");
        return nullptr;
    }

    try
    {
        // Build dependencies before their consumer: the walker's guard worker must be ready before the periodic
        // sampler can be started. These process-lifetime objects are published before CLR callbacks are enabled.
            auto allocationSamplingSessionProvider = std::make_unique<ClrAllocationSamplingSessionProvider>(info12_);
        auto stackWalker                       = std::make_unique<StackWalkerImpl>(info7_, runtime_);
            auto sampler = std::make_unique<ContinuousProfiler>(*allocationSamplingSessionProvider);
            if (info12_ != nullptr)
            {
                sampler->SetGlobalInfo12(info12_);
            }
            else
            {
                sampler->SetGlobalInfo7(info7_);
            }
        sampler->SetStackWalker(stackWalker.get());

            allocationSamplingSessionProvider_ = std::move(allocationSamplingSessionProvider);
        stackWalker_                       = std::move(stackWalker);
            sampler_                           = std::move(sampler);

#if defined(_WIN32)
        // .NET Framework may not report ThreadAssignedToOSThread for the managed thread that performs startup.
        ThreadID currentThreadId = 0;
        if (const auto hr = info7_->GetCurrentThreadID(&currentThreadId); SUCCEEDED(hr))
        {
            OnThreadAssignedToOSThread(currentThreadId, ::GetCurrentThreadId());
        }
#endif

        return sampler_.get();
    }
    catch (const std::exception& ex)
    {
        trace::Logger::Warn("RuntimeSamplerService: sampler creation failed: ", ex.what());
        return nullptr;
    }
    catch (...)
    {
        trace::Logger::Warn("RuntimeSamplerService: sampler creation failed with an unknown exception.");
        return nullptr;
    }
}

bool RuntimeSamplerService::EnsureRequiredClrEventsEnabled() noexcept
{
    if (requiredClrEventsEnabled_)
    {
        return true;
    }

    try
    {
        DWORD eventsLow;
        DWORD eventsHigh;
        auto  hr = info7_->GetEventMask2(&eventsLow, &eventsHigh);
        if (FAILED(hr))
        {
            trace::Logger::Warn("RuntimeSamplerService: failed to read CLR event masks. HRESULT=",
                                trace::HResultStr(hr));
            return false;
        }

        // Keep both capabilities latched after first enablement. Stack snapshots are passive until requested, while
        // thread callbacks keep thread metadata and .NET Framework canary state coherent across quiescent periods.
        eventsLow |= COR_PRF_MONITOR_THREADS | COR_PRF_ENABLE_STACK_SNAPSHOT;
        hr = info7_->SetEventMask2(eventsLow, eventsHigh);
        if (FAILED(hr))
        {
            trace::Logger::Warn("RuntimeSamplerService: failed to enable CLR thread-monitoring and stack-snapshot "
                                "events. HRESULT=",
                                trace::HResultStr(hr));
            return false;
        }

        requiredClrEventsEnabled_ = true;
        trace::Logger::Info("RuntimeSamplerService: CLR event masks configured for continuous profiler.");
        return true;
    }
    catch (const std::exception& ex)
    {
        trace::Logger::Warn("RuntimeSamplerService: enabling required CLR events failed: ", ex.what());
        return false;
    }
    catch (...)
    {
        trace::Logger::Warn("RuntimeSamplerService: enabling required CLR events failed with an unknown exception.");
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

void RuntimeSamplerService::PublishCommittedConfiguration(const RuntimeSamplerConfigurationV1& previousConfiguration,
                                                          const RuntimeSamplerConfigurationV1& configuration) noexcept
{
    // Post-commit publication only: no CLR calls, resource transitions, or blocking work belong here.
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
        // Shutdown may synchronously stop EventPipe and join the sampling thread.
        // Keep the controller lock scoped to the terminal state transition above.
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

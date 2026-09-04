/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_RUNTIME_SAMPLER_SERVICE_H_
#define OTEL_CLR_PROFILER_RUNTIME_SAMPLER_SERVICE_H_

#include <memory>
#include <mutex>

#include "cor.h"
#include "corprof.h"
#include "runtime_sampler_configuration.h"
#include "stack_capture_types.h"

namespace continuous_profiler
{

class ContinuousProfiler;
class ClrAllocationSamplingSessionProvider;
class StackWalkerImpl;

struct RuntimeSamplerApplyOutcome
{
    RuntimeSamplerApplyResult result;
    RuntimeSamplerState       state;
};

class RuntimeSamplerService final
{
public:
    RuntimeSamplerService(ICorProfilerInfo7* info7, ICorProfilerInfo12* info12, RuntimeType runtime);
    ~RuntimeSamplerService();

    RuntimeSamplerService(const RuntimeSamplerService&)            = delete;
    RuntimeSamplerService& operator=(const RuntimeSamplerService&) = delete;

    RuntimeSamplerApplyOutcome ApplyConfigurationV1(RuntimeSamplerAuthority              source,
                                                    const RuntimeSamplerConfigurationV1& configuration);
    RuntimeSamplerState        GetState() const;
    // Physical state is distinct from logical enablement: an all-disabled first commit remains dormant, while a
    // service disabled after first use retains its quiescent infrastructure for inexpensive re-enablement.
    bool HasSamplingInfrastructure() const;

    void Shutdown() noexcept;

    void OnThreadCreated(ThreadID threadId) noexcept;
    void OnThreadDestroyed(ThreadID threadId) noexcept;
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) noexcept;
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) noexcept;
    void OnAllocationTick(ULONG dataLength, LPCBYTE data) noexcept;

private:
    ContinuousProfiler* EnsureSamplerCreated() noexcept;
    bool                EnsureRequiredClrEventsEnabled() noexcept;
    bool EnsureSelectiveSamplingBuffersPrepared() noexcept;
    void PublishCommittedConfiguration(const RuntimeSamplerConfigurationV1& previousConfiguration,
                                       const RuntimeSamplerConfigurationV1& configuration) noexcept;

    ICorProfilerInfo7*                                    info7_   = nullptr;
    ICorProfilerInfo12*                                   info12_  = nullptr;
    RuntimeType                                           runtime_ = RuntimeType::Unknown;
    std::unique_ptr<ClrAllocationSamplingSessionProvider> allocationSamplingSessionProvider_;
    std::unique_ptr<ContinuousProfiler>                   sampler_;
    std::unique_ptr<StackWalkerImpl>                      stackWalker_;
    mutable std::mutex                                    configurationMutex_;
    RuntimeSamplerAuthority                               authority_ = RuntimeSamplerAuthority::None;
    RuntimeSamplerConfigurationV1 committedConfiguration_{sizeof(RuntimeSamplerConfigurationV1), 0, 0, 0};
    bool                          shutdownStarted_                  = false;
    bool                          requiredClrEventsEnabled_         = false;
    bool                          selectiveSamplingBuffersPrepared_ = false;
};

} // namespace continuous_profiler

#endif // OTEL_CLR_PROFILER_RUNTIME_SAMPLER_SERVICE_H_

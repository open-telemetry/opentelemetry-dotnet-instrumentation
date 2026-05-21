// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_capture_strategy_factory.h"
#include "suspension_policy.h"
#include "unified_stack_capture_strategy.h"

namespace continuous_profiler
{

std::unique_ptr<IStackCaptureStrategy> StackCaptureStrategyFactory::Create(ICorProfilerInfo2* profilerInfo,
                                                                           RuntimeType        runtimeType)
{
    auto  profilerApi    = std::make_unique<ProfilerStackCapture::ProfilerApiAdapter>(profilerInfo);
    auto* profilerApiRaw = profilerApi.get();

#if defined(_WIN32)
    auto  invocationQueue    = std::make_unique<ProfilerStackCapture::InvocationQueue>();
    auto* invocationQueueRaw = invocationQueue.get();
#else
    std::unique_ptr<ProfilerStackCapture::InvocationQueue> invocationQueue; // unused on POSIX
#endif

    if (runtimeType == RuntimeType::DotNetCore)
    {
        auto policy = std::make_unique<ProfilerStackCapture::ClrRuntimeSuspensionPolicy>(profilerApiRaw
#if defined(_WIN32)
                                                                                         ,
                                                                                         invocationQueueRaw
#endif
        );
        return std::make_unique<UnifiedStackCaptureStrategy>(std::move(profilerApi), std::move(invocationQueue),
                                                             std::move(policy));
    }

    if (runtimeType == RuntimeType::DotNetFramework)
    {
#if defined(_WIN32)
        auto policy =
            std::make_unique<ProfilerStackCapture::OsThreadSuspensionPolicy>(profilerApiRaw, invocationQueueRaw);
        return std::make_unique<UnifiedStackCaptureStrategy>(std::move(profilerApi), std::move(invocationQueue),
                                                             std::move(policy));
#else
        trace::Logger::Error("StackCaptureStrategyFactory: NetFx requires Windows");
        return nullptr;
#endif
    }
    return nullptr;
}

} // namespace continuous_profiler
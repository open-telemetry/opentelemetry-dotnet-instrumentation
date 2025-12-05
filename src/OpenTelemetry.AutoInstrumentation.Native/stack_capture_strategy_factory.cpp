// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_capture_strategy_factory.h"
#include "dot_net_stack_capture_strategy.h"
#ifdef _WIN64
#include "netfx_stack_capture_strategy.h"
#endif

namespace continuous_profiler
{

std::unique_ptr<IStackCaptureStrategy> StackCaptureStrategyFactory::Create(ICorProfilerInfo2* profilerInfo,
                                                                           const trace::RuntimeInformation& runtimeInfo)
{

    if (runtimeInfo.is_desktop())
    {
#ifdef _WIN64
        // trace::Logger::Info("StackCaptureStrategyFactory: Creating NetFxStackCaptureStrategy");
        return std::make_unique<NetFxStackCaptureStrategy>(profilerInfo);
#else
        // trace::Logger::Error("StackCaptureStrategyFactory: .NET Framework profiling not supported on 32-bit");
        return nullptr;
#endif
    }
    else
    {
        // trace::Logger::Info("StackCaptureStrategyFactory: Creating DotNetStackCaptureStrategy");
        //  Safe cast - we only get here if runtime is .NET Core 6+, which has ICorProfilerInfo12, comments above
        return std::make_unique<DotNetStackCaptureStrategy>(static_cast<ICorProfilerInfo12*>(profilerInfo));
    }
}

} // namespace continuous_profiler
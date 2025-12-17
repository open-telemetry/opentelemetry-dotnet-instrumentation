// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_capture_strategy_factory.h"
#include "dot_net_stack_capture_strategy.h"
#if defined(_WIN32) && defined(_M_AMD64)
#include "netfx_stack_capture_strategy_x64.h"
#endif

namespace continuous_profiler
{

std::unique_ptr<IStackCaptureStrategy> StackCaptureStrategyFactory::Create(ICorProfilerInfo2* profilerInfo,
                                                                           const trace::RuntimeInformation& runtimeInfo)
{

    if (runtimeInfo.is_desktop())
    {
#if defined(_WIN32) && defined(_M_AMD64)
        trace::Logger::Info("StackCaptureStrategyFactory: Creating NetFxStackCaptureStrategyX64");
        return std::make_unique<NetFxStackCaptureStrategyX64>(profilerInfo);
#else
        trace::Logger::Error("StackCaptureStrategyFactory: .NET Framework profiling not supported outside AMD64");
        return nullptr;
#endif
    }
    else
    {
        trace::Logger::Info("StackCaptureStrategyFactory: Creating DotNetStackCaptureStrategy");
        //  Safe cast - we only get here if runtime is .NET Core 6+, which has ICorProfilerInfo12, comments above
        return std::make_unique<DotNetStackCaptureStrategy>(static_cast<ICorProfilerInfo12*>(profilerInfo));
    }
}

} // namespace continuous_profiler
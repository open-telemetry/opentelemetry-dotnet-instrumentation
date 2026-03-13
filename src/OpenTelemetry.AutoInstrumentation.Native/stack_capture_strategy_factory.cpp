// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_capture_strategy_factory.h"
#include "dot_net_stack_capture_strategy.h"
#include "netfx_stack_capture_strategy.h"

namespace continuous_profiler
{

std::unique_ptr<IStackCaptureStrategy> StackCaptureStrategyFactory::Create(ICorProfilerInfo2* profilerInfo,
                                                                           const trace::RuntimeInformation& runtimeInfo)
{

    if (runtimeInfo.is_desktop())
    {
#if defined(_WIN32) && (defined(_M_AMD64) || defined(_M_IX86))
        trace::Logger::Info("StackCaptureStrategyFactory: Creating NetFxStackCaptureStrategy");
        return std::make_unique<NetFxStackCaptureStrategy>(profilerInfo);
#else
        trace::Logger::Error("StackCaptureStrategyFactory: .NET Framework profiling is only supported on x86 and x64");
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
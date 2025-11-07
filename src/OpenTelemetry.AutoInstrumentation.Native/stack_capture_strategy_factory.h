// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_STACK_CAPTURE_STRATEGY_FACTORY_H_
#define OTEL_PROFILER_STACK_CAPTURE_STRATEGY_FACTORY_H_

#include "clr_helpers.h"
#include "stack_capture_strategy.h"

namespace continuous_profiler {

/// @brief Factory for creating platform-specific stack capture strategies
class StackCaptureStrategyFactory {
public:
    /// @brief Creates appropriate strategy based on runtime information
    /// @param profilerInfo CLR profiler API (ICorProfilerInfo12 for .NET Core, ICorProfilerInfo7* for .NET FW)
    /// refer to:cor_profiler.cpp - line 105 - line 124 for details on why it is safe to cast to ICorProfilerInfo12
    /// @param runtimeInfo Runtime platform information
    /// @return Heap-allocated strategy (caller owns), or nullptr on unsupported platform
    static std::unique_ptr<IStackCaptureStrategy> Create(
        ICorProfilerInfo2* profilerInfo,
        const trace::RuntimeInformation& runtimeInfo);
// {
//        
//        if (runtimeInfo.is_desktop()) {
//#ifdef _WIN64
//            //trace::Logger::Info("StackCaptureStrategyFactory: Creating NetFxStackCaptureStrategy");
//            return std::make_unique<NetFxStackCaptureStrategy>(profilerInfo);
//#else
//            //trace::Logger::Error("StackCaptureStrategyFactory: .NET Framework profiling not supported on 32-bit");
//            return nullptr;
//#endif
//        } else {
//            //trace::Logger::Info("StackCaptureStrategyFactory: Creating DotNetStackCaptureStrategy");
//            // Safe cast - we only get here if runtime is .NET Core 6+, which has ICorProfilerInfo12, comments above
//            return std::make_unique<DotNetStackCaptureStrategy>(
//                static_cast<ICorProfilerInfo12*>(profilerInfo));
//        }
//    }
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_STACK_CAPTURE_STRATEGY_FACTORY_H_
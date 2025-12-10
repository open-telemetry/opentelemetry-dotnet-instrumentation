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
    /// @param runtimeInfo Runtime platform information
    /// @return Heap-allocated strategy (caller owns), or nullptr on unsupported platform
    static std::unique_ptr<IStackCaptureStrategy> Create(
        ICorProfilerInfo2* profilerInfo,
        const trace::RuntimeInformation& runtimeInfo);

};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_STACK_CAPTURE_STRATEGY_FACTORY_H_
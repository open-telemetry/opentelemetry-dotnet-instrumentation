// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_
#define OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_

#if defined(_WIN32) && defined(_M_AMD64)

#include "stack_capture_strategy.h"
#include "profiler_stack_capture.h"

namespace continuous_profiler {

/// @brief Stack capture strategy for .NET Framework
/// @details Uses thread suspension + seeded DoStackSnapshot via StackCaptureEngine
class NetFxStackCaptureStrategyX64 : public IStackCaptureStrategy {
public:
    explicit NetFxStackCaptureStrategyX64(ICorProfilerInfo2* profilerInfo)
        : engine_(std::make_unique<ProfilerStackCapture::StackCaptureEngine>(
              std::make_unique<ProfilerStackCapture::ProfilerApiAdapter>(profilerInfo))) {
        trace::Logger::Info("Initialized NetFxStackCaptureStrategyX64 (per-thread suspension)");
    }
    
    HRESULT CaptureStacks(
        const std::unordered_set<ThreadID>& threads,
        StackSnapshotCallbackContext* clientData) override {
        // StackCaptureEngine handles:
        // - Per-thread suspension via ScopedThreadSuspend
        // - Safety probes with canary thread
        // - Seeded DoStackSnapshot with PrepareContextForSnapshot
        return engine_->CaptureStacks(threads, clientData);
    }
    
    // Forward lifecycle events to StackCaptureEngine
    void OnThreadCreated(ThreadID threadId) override {
        if (engine_) {
            engine_->ThreadCreated(threadId);
        }
    }
    
    void OnThreadDestroyed(ThreadID threadId) override {
        if (engine_) {
            engine_->ThreadDestroyed(threadId);
        }
    }
    
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override {
        if (engine_ && name && cchName > 0) {
            engine_->ThreadNameChanged(threadId, cchName, name);
        }
    }
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override {
        if (engine_) {
            engine_->ThreadAssignedToOSThread(managedThreadId, osThreadId);
        }
    }
    
private:
    std::unique_ptr<ProfilerStackCapture::StackCaptureEngine> engine_;
};

} // namespace continuous_profiler

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_
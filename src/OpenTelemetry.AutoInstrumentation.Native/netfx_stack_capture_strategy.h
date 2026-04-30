// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_
#define OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_
// only Windows desktop CLR supports thread suspension and the associated stack capture strategy, so this entire file is
// ifdef'd out on unsupported platforms
#if defined(_WIN32)

#include "stack_capture_strategy.h"
#include "profiler_stack_capture.h"
#include "native_stack_walker.h"
#if defined(_M_AMD64)
#include "rtl_stack_walk.h"
#endif

namespace continuous_profiler {

/// @brief Stack capture strategy for .NET Framework
/// @details Uses thread suspension + DoStackSnapshot via StackCaptureEngine.
class NetFxStackCaptureStrategy : public IStackCaptureStrategy {
public:
    explicit NetFxStackCaptureStrategy(ICorProfilerInfo2* profilerInfo)
        : engine_(std::make_unique<ProfilerStackCapture::StackCaptureEngine>(
              std::make_unique<ProfilerStackCapture::ProfilerApiAdapter>(profilerInfo), 
            ProfilerStackCapture::CaptureOptions{},
              ProfilerStackCapture::CreateNativeStackWalker()
        )) {
        trace::Logger::Info("Initialized NetFxStackCaptureStrategy (per-thread suspension)");
    }
    
    HRESULT CaptureStacks(
        const std::unordered_set<ThreadID>& threads,
        void* clientData) override {
        // StackCaptureEngine handles:
        // - Per-thread suspension via ScopedThreadSuspend
        // - Safety probes with canary thread
        // - Seeded DoStackSnapshot with PrepareContextForSnapshot
        return engine_->CaptureStacks(threads, clientData);
    }
    HRESULT ResolveNativeSymbolName(UINT_PTR instructionPointer, trace::WSTRING& outName) override
    {
#ifdef _M_AMD64
        return ProfilerStackCapture::ResolveNativeSymbolName(instructionPointer, outName)
            ? S_OK : S_FALSE;
#else
        return E_NOTIMPL;
#endif
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

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_NETFX_STACK_CAPTURE_STRATEGY_H_
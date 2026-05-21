// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_STACK_WALKER_IMPL_H_
#define OTEL_STACK_WALKER_IMPL_H_

#include "stack_walker.h"
#include "captured_frame.h"
#include "stack_capture_strategy_factory.h"
#include "profiler_api.h"
#include <memory>

namespace continuous_profiler
{

/// @brief Concrete implementation that owns the strategy and exposes
/// IStackWalker + IThreadLifecycleListener as separate facets.
/// Because StackSnapshotCallbackContext embeds CapturedFrame, the bridge
/// passes the embedded frame by pointer with zero copies.
class StackWalkerImpl : public IStackWalker, public IThreadLifecycleListener
{
public:
    explicit StackWalkerImpl(ICorProfilerInfo2* profilerInfo, RuntimeType runtimeType)
        : strategy_(StackCaptureStrategyFactory::Create(profilerInfo, runtimeType))
    {
    }
    // -- IStackWalker (consumed by ContinuousProfiler) --
    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads,
                          StackCaptureRequest*                request) override
    {
        // Zero-copy bridge: forward the embedded CapturedFrame to the consumer.
        auto bridgeCallback =
            [request](ProfilerStackCapture::StackSnapshotCallbackContext* ctx) -> HRESULT
        {
            return request->onFrame(&ctx->frame);
        };

        ProfilerStackCapture::StackSnapshotCallbackContext context{bridgeCallback};
        return strategy_->CaptureStacks(threads, &context);
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR        instructionPointer,
                                    trace::WSTRING& outName) override
    {
        return strategy_->ResolveNativeSymbolName(instructionPointer, outName);
    }

    // -- IThreadLifecycleListener (consumed by CLR callback layer) --
    void OnThreadCreated(ThreadID threadId) override
    {
        strategy_->OnThreadCreated(threadId);
    }

    void OnThreadDestroyed(ThreadID threadId) override
    {
        strategy_->OnThreadDestroyed(threadId);
    }

    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override
    {
        strategy_->OnThreadNameChanged(threadId, cchName, name);
    }

    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override
    {
        strategy_->OnThreadAssignedToOSThread(managedThreadId, osThreadId);
    }

private:
    std::unique_ptr<IStackCaptureStrategy> strategy_;
};

} // namespace continuous_profiler

#endif // OTEL_STACK_WALKER_IMPL_H_
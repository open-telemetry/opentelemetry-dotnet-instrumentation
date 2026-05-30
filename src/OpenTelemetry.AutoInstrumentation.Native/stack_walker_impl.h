// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_STACK_WALKER_IMPL_H_
#define OTEL_STACK_WALKER_IMPL_H_

#include <memory>
#include <unordered_set>

#include "stack_walker.h"
#include "stack_capturer.h"

namespace continuous_profiler
{

/// @brief Concrete implementation that owns the stack capturer and exposes
/// IStackWalker + IThreadLifecycleListener as separate facets.
/// Because StackSnapshotCallbackContext embeds CapturedFrame, the bridge
/// passes the embedded frame by pointer with zero copies.
class StackWalkerImpl : public IStackWalker, public IThreadLifecycleListener
{
public:
    explicit StackWalkerImpl(ICorProfilerInfo2* profilerInfo, RuntimeType runtimeType)
        : capturer_(ProfilerStackCapture::CreateStackCapturer(profilerInfo, runtimeType))
    {
    }

    // -- IStackWalker (consumed by ContinuousProfiler) --
    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads,
                          StackCaptureRequest*                request) override
    {
        if (capturer_ == nullptr)
        {
            return E_FAIL;
        }
        if (request == nullptr || !request->onFrame)
        {
            return E_INVALIDARG;
        }

        // Zero-copy bridge: forward the embedded CapturedFrame to the consumer.
        auto bridgeCallback =
            [request](ProfilerStackCapture::StackSnapshotCallbackContext* ctx) -> HRESULT
        {
            return request->onFrame(&ctx->frame);
        };

        ProfilerStackCapture::StackSnapshotCallbackContext context{bridgeCallback};
        return capturer_->CaptureStacks(threads, &context);
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR        instructionPointer,
                                    trace::WSTRING& outName) override
    {
        return capturer_ ? capturer_->ResolveNativeSymbolName(instructionPointer, outName) : E_FAIL;
    }

    // -- IThreadLifecycleListener (consumed by CLR callback layer) --
    void OnThreadCreated(ThreadID threadId) override
    {
        if (capturer_)
        {
            capturer_->OnThreadCreated(threadId);
        }
    }

    void OnThreadDestroyed(ThreadID threadId) override
    {
        if (capturer_)
        {
            capturer_->OnThreadDestroyed(threadId);
        }
    }

    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override
    {
        if (capturer_)
        {
            capturer_->OnThreadNameChanged(threadId, cchName, name);
        }
    }

    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override
    {
        if (capturer_)
        {
            capturer_->OnThreadAssignedToOSThread(managedThreadId, osThreadId);
        }
    }

private:
    using IStackCapturer = ProfilerStackCapture::IStackCapturer;
    std::unique_ptr<IStackCapturer> capturer_;
};

} // namespace continuous_profiler

#endif // OTEL_STACK_WALKER_IMPL_H_
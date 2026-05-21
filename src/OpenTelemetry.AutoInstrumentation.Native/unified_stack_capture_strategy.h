// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_UNIFIED_STACK_CAPTURE_STRATEGY_H_
#define OTEL_PROFILER_UNIFIED_STACK_CAPTURE_STRATEGY_H_


#include <memory>
#include <unordered_set>

#include "logger.h"
#include "profiler_api.h"
#include "stack_capture_strategy.h"
#include "suspension_policy.h"

#if defined(_WIN32) && defined(_M_AMD64)
#include "safe_native_walk_service.h"
#endif

namespace continuous_profiler
{

class UnifiedStackCaptureStrategy final : public IStackCaptureStrategy
{
public:
    UnifiedStackCaptureStrategy(std::unique_ptr<ProfilerStackCapture::IProfilerApi> profilerApi,
                                std::unique_ptr<ProfilerStackCapture::InvocationQueue> invocationQueue,
                                std::unique_ptr<ProfilerStackCapture::ISuspensionPolicy> suspensionPolicy);
    ~UnifiedStackCaptureStrategy() override;

    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads,
                          void* clientData) override;

    HRESULT ResolveNativeSymbolName(UINT_PTR instructionPointer,
                                    trace::WSTRING& outName) override;

    void OnThreadCreated(ThreadID threadId) override;
    void OnThreadDestroyed(ThreadID threadId) override;
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;

private:
    class BatchSuspensionGuard
    {
    public:
        explicit BatchSuspensionGuard(ProfilerStackCapture::ISuspensionPolicy* policy);
        ~BatchSuspensionGuard();

        BatchSuspensionGuard(const BatchSuspensionGuard&) = delete;
        BatchSuspensionGuard& operator=(const BatchSuspensionGuard&) = delete;

        HRESULT Result() const;

    private:
        ProfilerStackCapture::ISuspensionPolicy* policy_;
        HRESULT hr_;
        bool active_;
    };

    HRESULT CaptureStack(ThreadID managedThreadId,
                         ProfilerStackCapture::StackSnapshotCallbackContext* clientData);

    HRESULT TryNativeWalkAndSeed(ThreadID managedThreadId,
                                 DWORD osThreadId,
                                 ProfilerStackCapture::IThreadCaptureScope* threadScope,
                                 ProfilerStackCapture::StackSnapshotCallbackContext* clientData);
    // -----------------------------------------------------------------------
    // Member declaration order is load-bearing - DO NOT REORDER.
    //
    // Reverse-destruction order on shutdown:
    //   nativeWalkService_  -> stops issuing probes / native walks
    //   suspensionPolicy_   -> stops issuing probes
    //   invocationQueue_    -> ~InvocationQueue joins the worker thread,
    //                          guaranteeing no in-flight probe lambda is
    //                          still running.
    //   profilerApi_        -> safe to destroy: no worker references it.
    //
    // Both suspensionPolicy_ (via its StackSafetyProbe) and
    // nativeWalkService_ (via its StackSafetyProbe) submit lambdas to
    // invocationQueue_ that capture profilerApi_ by raw pointer.  The
    // declaration order above is the only thing preventing a shutdown
    // use-after-free.
    // -----------------------------------------------------------------------

    std::unique_ptr<ProfilerStackCapture::IProfilerApi> profilerApi_;
    std::unique_ptr<ProfilerStackCapture::InvocationQueue>   invocationQueue_;
    std::unique_ptr<ProfilerStackCapture::ISuspensionPolicy> suspensionPolicy_;

#if defined(_WIN32) && defined(_M_AMD64)
    std::unique_ptr<ProfilerStackCapture::SafeNativeWalkService> nativeWalkService_;
#endif
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_UNIFIED_STACK_CAPTURE_STRATEGY_H_
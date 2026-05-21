// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "unified_stack_capture_strategy.h"

namespace continuous_profiler
{

UnifiedStackCaptureStrategy::BatchSuspensionGuard::BatchSuspensionGuard(ProfilerStackCapture::ISuspensionPolicy* policy)
    : policy_(policy), hr_(policy_->BeginBatch()), active_(SUCCEEDED(hr_))
{
}

UnifiedStackCaptureStrategy::BatchSuspensionGuard::~BatchSuspensionGuard()
{
    if (active_)
    {
        policy_->EndBatch();
    }
}

HRESULT UnifiedStackCaptureStrategy::BatchSuspensionGuard::Result() const
{
    return hr_;
}

UnifiedStackCaptureStrategy::UnifiedStackCaptureStrategy(
    std::unique_ptr<ProfilerStackCapture::IProfilerApi>      profilerApi,
    std::unique_ptr<ProfilerStackCapture::InvocationQueue>   invocationQueue,
    std::unique_ptr<ProfilerStackCapture::ISuspensionPolicy> suspensionPolicy)
    : profilerApi_(std::move(profilerApi))
    , invocationQueue_(std::move(invocationQueue))
    , suspensionPolicy_(std::move(suspensionPolicy))
{
#if defined(_WIN32) && defined(_M_AMD64)
    nativeWalkService_ = std::make_unique<ProfilerStackCapture::SafeNativeWalkService>(profilerApi_.get());
#endif
}

UnifiedStackCaptureStrategy::~UnifiedStackCaptureStrategy()
{
    // Early signal: stop the policy's canary wait and any pending probes.
    // The actual worker thread join happens in ~InvocationQueue, which runs
    // AFTER suspensionPolicy_ and nativeWalkService_ have been destroyed
    // thanks to declaration order.
    if (suspensionPolicy_)
        suspensionPolicy_->Stop();
    if (invocationQueue_)
        invocationQueue_->Stop();
}

HRESULT UnifiedStackCaptureStrategy::CaptureStacks(const std::unordered_set<ThreadID>& threads, void* clientData)
{
    if (threads.empty())
    {
        return S_OK;
    }

    auto* callbackContext = static_cast<ProfilerStackCapture::StackSnapshotCallbackContext*>(clientData);

    try
    {
        BatchSuspensionGuard batch(suspensionPolicy_.get());
        if (FAILED(batch.Result()))
        {
            trace::Logger::Error("[UnifiedStackCaptureStrategy] BeginBatch failed. HRESULT=",
                                 trace::HResultStr(batch.Result()));
            return batch.Result();
        }

        if (!suspensionPolicy_->PrepareForCapture(threads))
        {
            trace::Logger::Debug("[UnifiedStackCaptureStrategy] Policy declined capture preparation.");
            return E_FAIL;
        }

        HRESULT captureResult = S_OK;

        for (ThreadID managedThreadId : threads)
        {
            if (suspensionPolicy_->ShouldSkipThread(managedThreadId))
            {
                continue;
            }

            DWORD osThreadId = 0;
            if (suspensionPolicy_->RequiresOsThreadIdForManagedSnapshot())
            {
                HRESULT osHr = suspensionPolicy_->GetOsThreadId(managedThreadId, &osThreadId);
                if (FAILED(osHr) || osThreadId == 0)
                {
                    trace::Logger::Debug("[UnifiedStackCaptureStrategy] Failed to get OS thread ID. ManagedID=",
                                         managedThreadId, ", HRESULT=", trace::HResultStr(osHr));
                    continue;
                }
            }

            std::unique_ptr<ProfilerStackCapture::IThreadCaptureScope> threadScope;
            try
            {
                threadScope = suspensionPolicy_->AcquireThread(osThreadId);
            }
            catch (const std::exception& ex)
            {
                trace::Logger::Debug("[UnifiedStackCaptureStrategy] Failed to acquire thread. ManagedID=",
                                     managedThreadId, ", NativeID=", osThreadId, ": ", ex.what());
                continue;
            }

            if (!suspensionPolicy_->BeforeStackSnapshot())
            {
                trace::Logger::Debug(
                    "[UnifiedStackCaptureStrategy] Skipping thread due to safety probe failure. ManagedID=",
                    managedThreadId, ", NativeID=", osThreadId);
                continue;
            }

            callbackContext->frame.threadId = managedThreadId;

            HRESULT frameHr = CaptureStack(managedThreadId, callbackContext);
            if (FAILED(frameHr))
            {
                frameHr = TryNativeWalkAndSeed(managedThreadId, osThreadId, threadScope.get(), callbackContext);
            }

            if (FAILED(frameHr) && SUCCEEDED(captureResult))
            {
                captureResult = frameHr;
            }
        }

        return SUCCEEDED(captureResult) ? S_OK : captureResult;
    }
    catch (const std::exception& ex)
    {
        trace::Logger::Error("[UnifiedStackCaptureStrategy] Exception during CaptureStacks: ", ex.what());
        return E_FAIL;
    }
}

HRESULT UnifiedStackCaptureStrategy::CaptureStack(ThreadID                                            managedThreadId,
                                                  ProfilerStackCapture::StackSnapshotCallbackContext* clientData)
{
    HRESULT hr = profilerApi_->DoStackSnapshotUnseeded(managedThreadId, clientData);

    if (SUCCEEDED(hr))
    {
        trace::Logger::Debug("[UnifiedStackCaptureStrategy] Unseeded capture succeeded. ThreadID=", managedThreadId);
    }
    else
    {
        trace::Logger::Debug("[UnifiedStackCaptureStrategy] Unseeded capture failed (0x", std::hex, hr, std::dec,
                             "). ThreadID=", managedThreadId);
    }

    return hr;
}

HRESULT UnifiedStackCaptureStrategy::TryNativeWalkAndSeed(
    ThreadID                                            managedThreadId,
    DWORD                                               osThreadId,
    ProfilerStackCapture::IThreadCaptureScope*          threadScope,
    ProfilerStackCapture::StackSnapshotCallbackContext* clientData)
{
#if defined(_WIN32) && defined(_M_AMD64)
    if (!nativeWalkService_)
    {
        return E_NOTIMPL;
    }

    if (threadScope && threadScope->HasSuspendedThreadHandle())
    {
        // OsThreadSuspensionPolicy: thread already OS-suspended, reuse handle directly.
        return nativeWalkService_->CaptureSuspendedThread(threadScope->GetThreadHandle(), managedThreadId, clientData);
    }
    // ClrRuntimeSuspensionPolicy: runtime is suspended but no OS handle held.
    // osThreadId was not fetched upfront (RequiresOsThreadIdForManagedSnapshot = false),
    // fetch it now while SuspendRuntime is still active - managed threads won't be rescheduled during the suspension
    // window, so this is safe.
    if (osThreadId == 0)
    {
        if (auto hr = suspensionPolicy_->GetOsThreadId(managedThreadId, &osThreadId); FAILED(hr) || osThreadId == 0)
        {
            trace::Logger::Debug("[UnifiedStackCaptureStrategy] Failed to get OS thread ID for native walk. ManagedID=",
                                 managedThreadId, ", HRESULT=", trace::HResultStr(hr));
            return E_FAIL;
        }
    }
    return nativeWalkService_->CaptureRunningThread(osThreadId, managedThreadId, clientData);
#else
    (void)managedThreadId;
    (void)osThreadId;
    (void)threadScope;
    (void)clientData;
    return E_NOTIMPL;
#endif
}

HRESULT UnifiedStackCaptureStrategy::ResolveNativeSymbolName(UINT_PTR instructionPointer, trace::WSTRING& outName)
{
#if defined(_WIN32) && defined(_M_AMD64)
    if (nativeWalkService_)
    {
        return nativeWalkService_->GetSymbolResolver().Resolve(instructionPointer, outName) ? S_OK : S_FALSE;
    }
#endif

    (void)instructionPointer;
    (void)outName;
    return E_NOTIMPL;
}

void UnifiedStackCaptureStrategy::OnThreadCreated(ThreadID threadId)
{
    suspensionPolicy_->OnThreadCreated(threadId);
}

void UnifiedStackCaptureStrategy::OnThreadDestroyed(ThreadID threadId)
{
    suspensionPolicy_->OnThreadDestroyed(threadId);
}

void UnifiedStackCaptureStrategy::OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    suspensionPolicy_->OnThreadNameChanged(threadId, cchName, name);
}

void UnifiedStackCaptureStrategy::OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    suspensionPolicy_->OnThreadAssignedToOSThread(managedThreadId, osThreadId);
}

} // namespace continuous_profiler

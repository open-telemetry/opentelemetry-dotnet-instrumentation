// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_
#define OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_

// Windows-only .NET Framework runtime capture.
//
// Suspension model is per-thread (Win32 SuspendThread), driven inside
// CaptureStack via ThreadGuard.  SuspendRuntime/ResumeRuntime are no-ops
// at the runtime level.
//
// Safety: per CaptureStack call we suspend the target, run the NetFx
// probe set (HeapLock + CanaryDSS, plus Rtl on x64) against the canary
// thread, then issue DSS.  On x64, if seedless DSS fails we fall back
// to native walk + seeded DSS reusing the already-suspended target;
// the probes done before DSS still cover the lock-state invariant for
// the fallback path because the target stays suspended.

#if defined(_WIN32)

#include <atomic>
#include <chrono>
#include <map>
#include <memory>
#include <mutex>
#include <string>

#include "runtime_capture.h"
#include "stack_walk_guard.h"

#if defined(_M_AMD64)
#include "safe_native_walk_service.h"
#endif

namespace ProfilerStackCapture
{
struct CanarySnapshot
{
    ThreadID managedId = 0;
    DWORD    osId      = 0;

    bool IsValid() const
    {
        return managedId != 0 && osId != 0;
    }
};

struct NetFxCaptureOptions
{
    std::chrono::milliseconds probeTimeout    = std::chrono::milliseconds(250);
    const wchar_t*            canaryNamePrefix = L"OpenTelemetry Profiler Canary Thread";

    bool IsCanaryThreadName(const std::wstring& threadName) const
    {
        return threadName.find(canaryNamePrefix) == 0;
    }
};

class NetFxRuntimeCapture final : public IRuntimeCapture
{
public:
    explicit NetFxRuntimeCapture(IProfilerApi*              profilerApi,
                                 const NetFxCaptureOptions& options = {});
    ~NetFxRuntimeCapture() override;

    NetFxRuntimeCapture(const NetFxRuntimeCapture&)            = delete;
    NetFxRuntimeCapture& operator=(const NetFxRuntimeCapture&) = delete;

    HRESULT SuspendRuntime() override { return S_OK; }
    void    ResumeRuntime() noexcept override {}

    HRESULT CaptureStack(ThreadID                      managedThreadId,
                         StackSnapshotCallbackContext* clientData) override;

    void OnThreadDestroyed(ThreadID threadId) override;
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;

    void Stop() override;

private:
    CanarySnapshot SnapshotCanary() const;
    void           ReelectCanaryLocked();

    IProfilerApi*                          profilerApi_;
    NetFxCaptureOptions                    options_;
    std::unique_ptr<StackWalkGuard>        stackWalkGuard_;
#if defined(_M_AMD64)
    std::unique_ptr<SafeNativeWalkService> nativeWalk_;
#endif

    mutable std::mutex               mutex_;
    std::map<ThreadID, DWORD>        activeThreads_;
    std::map<ThreadID, std::wstring> threadNames_;
    CanarySnapshot                   canary_;

    std::atomic<bool> stopRequested_{false};
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_
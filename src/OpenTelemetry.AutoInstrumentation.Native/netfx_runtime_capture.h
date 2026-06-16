// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_
#define OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_

// Windows-only .NET Framework runtime capture.
//
// Suspension model is per-thread (Win32 SuspendThread), driven inside
// CaptureStack via ThreadGuard.  SuspendRuntime/ResumeRuntime are no-ops
// at the runtime level because NetFx has no profiler-driven global
// suspension API.
//
// Safety: per CaptureStack call we suspend both the target and a canary
// thread, then drive probes through StackWalkGuard:
//
//   x64 path:
//     1. RTL frame-0 probe (RtlLookupFunctionEntry + GetFunctionFromIP +
//        loader-lock probe + RtlVirtualUnwind). Proves the target holds
//        none of the hazardous CSes AND produces a classified frame-0
//        with an unwound CONTEXT.
//     2. Canary DSS probe. Certifies process-wide DoStackSnapshot health
//        (required because NetFx lacks a runtime-suspension shield).
//     3. Branch on frame-0 verdict:
//          - Managed: ctx is the seed (probe left it untouched) -> seeded
//            DSS directly.
//          - Native: emit composed frame-0, walk natively from the
//            unwound ctx (frame-1) until a managed boundary, then seed
//            DSS from there.
//     Safety of frames 1..N follows by induction: the frame-0 probe
//     proved the target holds no RTL/CLR/loader CSes, and the target
//     remains suspended - it cannot subsequently acquire any.
//
//   x86 path:
//     1. STL/heap gate + canary DSS probe (no RTL machinery on x86).
//     2. Seedless DSS (only safe option without unwind APIs).

#if defined(_WIN32)

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
    ~NetFxRuntimeCapture() = default;

    NetFxRuntimeCapture(const NetFxRuntimeCapture&)            = delete;
    NetFxRuntimeCapture& operator=(const NetFxRuntimeCapture&) = delete;

    HRESULT SuspendRuntime() override { return S_OK; }
    void    ResumeRuntime() noexcept override {}

    HRESULT CaptureStack(ThreadID                      managedThreadId,
                         StackSnapshotCallbackContext* clientData) override;

    void OnThreadDestroyed(ThreadID threadId) override;
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;


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
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_NETFX_RUNTIME_CAPTURE_H_
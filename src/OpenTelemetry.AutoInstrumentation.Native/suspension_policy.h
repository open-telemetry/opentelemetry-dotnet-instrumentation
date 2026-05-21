// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_SUSPENSION_POLICY_H_
#define OTEL_PROFILER_SUSPENSION_POLICY_H_

#include <atomic>
#include <memory>
#include <unordered_set>
#include <corhlpr.h>
#include <corprof.h>

#include "profiler_api.h"

#if defined(_WIN32)
#include <chrono>
#include <condition_variable>
#include <map>
#include <mutex>
#include <string>
#include "stack_safety_probe.h"
#include "thread_suspend.h"
#endif

namespace ProfilerStackCapture
{

class IThreadCaptureScope
{
public:
    virtual ~IThreadCaptureScope() = default;

#if defined(_WIN32)
    virtual HANDLE GetThreadHandle() const = 0;
    virtual bool HasSuspendedThreadHandle() const = 0;
#endif
};

class NoopThreadCaptureScope final : public IThreadCaptureScope
{
public:
#if defined(_WIN32)
    HANDLE GetThreadHandle() const override
    {
        return INVALID_HANDLE_VALUE;
    }

    bool HasSuspendedThreadHandle() const override
    {
        return false;
    }
#endif
};

class ISuspensionPolicy
{
public:
    virtual ~ISuspensionPolicy() = default;

    virtual HRESULT BeginBatch() = 0;
    virtual void EndBatch() noexcept = 0;

    virtual bool PrepareForCapture(const std::unordered_set<ThreadID>& threads) = 0;
    virtual bool ShouldSkipThread(ThreadID managedThreadId) const = 0;

    virtual bool RequiresOsThreadIdForManagedSnapshot() const = 0;
    virtual HRESULT GetOsThreadId(ThreadID managedThreadId, DWORD* osThreadId) = 0;

    virtual std::unique_ptr<IThreadCaptureScope> AcquireThread(DWORD osThreadId) = 0;

    virtual bool BeforeStackSnapshot() = 0;

    virtual void Stop() {}

    virtual void OnThreadCreated(ThreadID threadId) {}
    virtual void OnThreadDestroyed(ThreadID threadId) {}
    virtual void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) {}
    virtual void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) {}
};

class ClrRuntimeSuspensionPolicy final : public ISuspensionPolicy
{
public:
    explicit ClrRuntimeSuspensionPolicy(IProfilerApi* profilerApi, 
        InvocationQueue*      queue   = nullptr
                                        );

    HRESULT BeginBatch() override;
    void EndBatch() noexcept override;

    bool PrepareForCapture(const std::unordered_set<ThreadID>& threads) override;
    bool ShouldSkipThread(ThreadID managedThreadId) const override;

    bool RequiresOsThreadIdForManagedSnapshot() const override;
    HRESULT GetOsThreadId(ThreadID managedThreadId, DWORD* osThreadId) override;

    std::unique_ptr<IThreadCaptureScope> AcquireThread(DWORD osThreadId) override;

    bool BeforeStackSnapshot() override;

private:
    IProfilerApi* profilerApi_;

#if defined(_WIN32) && defined(_M_AMD64)
    // Probes loader/heap locks that the native-walk fallback will touch on
    // suspended target threads.  winx86 has no native walk, so no probe.
    std::unique_ptr<StackSafetyProbe> safetyProbe_;
#endif
};

#if defined(_WIN32)

struct CaptureOptions
{
    std::chrono::milliseconds probeTimeout = std::chrono::milliseconds(250);
    const wchar_t* canaryThreadName = L"OpenTelemetry Profiler Canary Thread";

    bool IsCanaryThread(const std::wstring& threadName) const
    {
        return threadName.find(canaryThreadName) == 0;
    }
};

struct CanaryThreadInfo
{
    ThreadID managedId = 0;
    DWORD nativeId = 0;

    void reset()
    {
        managedId = 0;
        nativeId = 0;
    }

    bool isValid() const
    {
        return managedId != 0 && nativeId != 0;
    }
};

class ScopedThreadCaptureScope final : public IThreadCaptureScope
{
public:
    explicit ScopedThreadCaptureScope(DWORD osThreadId)
        : suspendedThread_(osThreadId)
    {
    }

    HANDLE GetThreadHandle() const override
    {
        return suspendedThread_.GetHandle();
    }

    bool HasSuspendedThreadHandle() const override
    {
        return suspendedThread_.IsValid();
    }

private:
    ScopedThreadSuspend suspendedThread_;
};

class OsThreadSuspensionPolicy final : public ISuspensionPolicy
{
public:
    explicit OsThreadSuspensionPolicy(IProfilerApi* profilerApi,
                                      InvocationQueue*      queue = nullptr,
                                      const CaptureOptions& options = {});
    ~OsThreadSuspensionPolicy() override;

    HRESULT BeginBatch() override;
    void EndBatch() noexcept override;

    bool PrepareForCapture(const std::unordered_set<ThreadID>& threads) override;
    bool ShouldSkipThread(ThreadID managedThreadId) const override;

    bool RequiresOsThreadIdForManagedSnapshot() const override;
    HRESULT GetOsThreadId(ThreadID managedThreadId, DWORD* osThreadId) override;

    std::unique_ptr<IThreadCaptureScope> AcquireThread(DWORD osThreadId) override;
    bool BeforeStackSnapshot() override;

    void Stop() override;

    void OnThreadDestroyed(ThreadID threadId) override;
    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;

private:
    CanaryThreadInfo WaitForCanaryThread(std::chrono::milliseconds timeout = std::chrono::milliseconds(0));
    bool SafetyProbe(const CanaryThreadInfo& canaryInfo);

    IProfilerApi* profilerApi_;
    CaptureOptions options_;
    std::atomic<bool> stopRequested_{};

    mutable std::mutex threadListMutex_;
    std::map<ThreadID, DWORD> activeThreads_;
    std::map<ThreadID, std::wstring> threadNames_;
    CanaryThreadInfo canaryThread_;

    std::condition_variable captureCondVar_;
    std::unique_ptr<StackSafetyProbe> safetyProbe_;
};

#endif

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_SUSPENSION_POLICY_H_
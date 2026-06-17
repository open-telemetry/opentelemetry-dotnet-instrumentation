// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_THREAD_SUSPEND_H_
#define OTEL_PROFILER_THREAD_SUSPEND_H_

// RAII Win32 thread suspension used by both native stack walking (RTL) and
// the .NET Framework per-thread stack capture strategy.

#if defined(_WIN32)

#include <windows.h>

namespace ProfilerStackCapture
{
/// <summary>
/// Win32 thread handle validity check.
///
/// OpenThread / OpenProcess return NULL on failure (not INVALID_HANDLE_VALUE);
/// CreateFile-family APIs return INVALID_HANDLE_VALUE.  Code that consumes
/// either family must reject both sentinels to be correct.
/// </summary>
inline bool IsValidThreadHandle(HANDLE h) noexcept
{
    return h != nullptr && h != INVALID_HANDLE_VALUE;
}


/// @brief RAII guard for Win32 SuspendThread / ResumeThread.
/// Opens the thread handle, suspends on construction, resumes on destruction.
/// Move-only to prevent double-resume.
class ScopedThreadSuspend
{
public:
    explicit ScopedThreadSuspend(DWORD nativeThreadId);
    ~ScopedThreadSuspend();
    ScopedThreadSuspend(const ScopedThreadSuspend&)            = delete;
    ScopedThreadSuspend& operator=(const ScopedThreadSuspend&) = delete;
    ScopedThreadSuspend(ScopedThreadSuspend&&) noexcept;
    ScopedThreadSuspend& operator=(ScopedThreadSuspend&&) noexcept;

    bool IsSuspended() const noexcept;

    // Captures CONTEXT of the suspended thread. Returns false if not suspended
    // or if GetThreadContext fails. Safe to call only while guard is live.
    bool GetContext(CONTEXT& ctx) const noexcept;

private:

    HANDLE threadHandle_ = nullptr;
    bool   suspended_    = false;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_THREAD_SUSPEND_H_
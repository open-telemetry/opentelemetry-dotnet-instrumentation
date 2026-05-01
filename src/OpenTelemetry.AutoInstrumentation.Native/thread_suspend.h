// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_THREAD_SUSPEND_H_
#define OTEL_THREAD_SUSPEND_H_

// RAII Win32 thread suspension used by both native stack walking (RTL) and
// the .NET Framework per-thread stack capture strategy.
// Windows x64 only.

#if defined(_WIN32)

#include <windows.h>

namespace ProfilerStackCapture
{

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
    ScopedThreadSuspend(ScopedThreadSuspend&& other) noexcept;
    ScopedThreadSuspend& operator=(ScopedThreadSuspend&& other) noexcept;
    HANDLE GetHandle() const { return threadHandle_; }
    bool   IsValid() const { return threadHandle_ != INVALID_HANDLE_VALUE; }

private:
    HANDLE threadHandle_;
    bool   suspended_;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_THREAD_SUSPEND_H_
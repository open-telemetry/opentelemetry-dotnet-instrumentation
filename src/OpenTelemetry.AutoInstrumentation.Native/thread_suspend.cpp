// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32)

#include "thread_suspend.h"

#include <stdexcept>

namespace ProfilerStackCapture
{

ScopedThreadSuspend::ScopedThreadSuspend(DWORD nativeThreadId) : threadHandle_(INVALID_HANDLE_VALUE), suspended_(false)
{
    threadHandle_ = OpenThread(THREAD_GET_CONTEXT | THREAD_SUSPEND_RESUME, FALSE, nativeThreadId);
    if (threadHandle_ == NULL)
    {
        throw std::runtime_error("Failed to open thread handle");
    }

    DWORD suspendCount = SuspendThread(threadHandle_);
    if (suspendCount == static_cast<DWORD>(-1))
    {
        CloseHandle(threadHandle_);
        threadHandle_ = INVALID_HANDLE_VALUE;
        throw std::runtime_error("Failed to suspend thread");
    }

    suspended_ = true;
}

ScopedThreadSuspend::~ScopedThreadSuspend()
{
    if (threadHandle_ != INVALID_HANDLE_VALUE)
    {
        if (suspended_)
        {
            ResumeThread(threadHandle_);
        }

        CloseHandle(threadHandle_);
    }
}

ScopedThreadSuspend::ScopedThreadSuspend(ScopedThreadSuspend&& other) noexcept
    : threadHandle_(other.threadHandle_), suspended_(other.suspended_)
{
    other.threadHandle_ = INVALID_HANDLE_VALUE;
    other.suspended_    = false;
}

ScopedThreadSuspend& ScopedThreadSuspend::operator=(ScopedThreadSuspend&& other) noexcept
{
    if (this != &other)
    {
        if (threadHandle_ != INVALID_HANDLE_VALUE)
        {
            if (suspended_)
            {
                ResumeThread(threadHandle_);
            }

            CloseHandle(threadHandle_);
        }

        threadHandle_       = other.threadHandle_;
        suspended_          = other.suspended_;
        other.threadHandle_ = INVALID_HANDLE_VALUE;
        other.suspended_    = false;
    }

    return *this;
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
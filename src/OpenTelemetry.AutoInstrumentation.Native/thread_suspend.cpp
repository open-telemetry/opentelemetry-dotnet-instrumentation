// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32)

#include "thread_suspend.h"

namespace ProfilerStackCapture
{

ScopedThreadSuspend::ScopedThreadSuspend(DWORD nativeThreadId)
    : threadHandle_(OpenThread(THREAD_GET_CONTEXT | THREAD_SUSPEND_RESUME, FALSE, nativeThreadId))
{
    if (!IsValidThreadHandle(threadHandle_))
    {
        return;
    }

    suspended_ = SuspendThread(threadHandle_) != static_cast<DWORD>(-1);
    if (!suspended_)
    {
        CloseHandle(threadHandle_);
        threadHandle_ = INVALID_HANDLE_VALUE;
    }
}

ScopedThreadSuspend::~ScopedThreadSuspend()
{
    if (IsValidThreadHandle(threadHandle_))
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
        if (IsValidThreadHandle(threadHandle_))
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

bool ScopedThreadSuspend::IsSuspended() const noexcept
{
    return suspended_;
}
bool ScopedThreadSuspend::GetContext(CONTEXT& ctx) const noexcept
{
    if (!suspended_ || !IsValidThreadHandle(threadHandle_))
    {
        return false;
    }
    ctx.ContextFlags = CONTEXT_FULL;

    return GetThreadContext(threadHandle_, &ctx) != 0;
}
} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
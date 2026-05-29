// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_SUSPENSION_GUARDS_H_
#define OTEL_PROFILER_SUSPENSION_GUARDS_H_

// std::lock_guard-shaped RAII helpers for runtime-wide and per-thread
// suspension used by the runtime-capture implementations
// (ClrRuntimeCapture / NetFxRuntimeCapture).

#include <corhlpr.h>
#include <corprof.h>

#if defined(_WIN32)
#include <windows.h>
#include "thread_suspend.h"
#endif

namespace ProfilerStackCapture
{

class IRuntimeCapture;



class RuntimeGuard
{
public:
    explicit RuntimeGuard(IRuntimeCapture* runtime);
    ~RuntimeGuard();

    RuntimeGuard(const RuntimeGuard&)            = delete;
    RuntimeGuard& operator=(const RuntimeGuard&) = delete;
    RuntimeGuard(RuntimeGuard&&)                 = delete;
    RuntimeGuard& operator=(RuntimeGuard&&)      = delete;

    bool IsActive() const { return active_; }

private:
    IRuntimeCapture* runtime_;
    bool             active_;
};

#if defined(_WIN32)
class ThreadGuard
{
public:
    explicit ThreadGuard(DWORD osThreadId) : suspended_(osThreadId) {}

    ThreadGuard(const ThreadGuard&)            = delete;
    ThreadGuard& operator=(const ThreadGuard&) = delete;

    bool IsAcquired() const
    {
        return IsValidThreadHandle(suspended_.GetHandle());
    }
    HANDLE GetHandle() const { return suspended_.GetHandle(); }

private:
    ScopedThreadSuspend suspended_;
};
#endif

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_SUSPENSION_GUARDS_H_
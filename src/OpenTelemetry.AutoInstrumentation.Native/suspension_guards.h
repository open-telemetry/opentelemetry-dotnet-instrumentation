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
    bool active_ = false;
};

#if defined(_WIN32)
class ThreadGuard
{
public:
    explicit ThreadGuard(DWORD osThreadId) : suspended_(osThreadId) {}

    ThreadGuard(const ThreadGuard&)            = delete;
    ThreadGuard& operator=(const ThreadGuard&) = delete;
    ThreadGuard(ThreadGuard&&)                 = delete;
    ThreadGuard& operator=(ThreadGuard&&)      = delete;

    bool IsAcquired() const { return suspended_.IsSuspended(); }

    // Delegates to ScopedThreadSuspend - safe only while guard is live
    bool GetContext(CONTEXT& ctx) const noexcept { return suspended_.GetContext(ctx); }

private:
    ScopedThreadSuspend suspended_;
};
#endif

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_SUSPENSION_GUARDS_H_
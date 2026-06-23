// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_STACK_CAPTURER_H_
#define OTEL_PROFILER_STACK_CAPTURER_H_

#include <memory>
#include <unordered_set>
#include <corhlpr.h>
#include <corprof.h>

#include "stack_capture_types.h"
#include "profiler_api.h"
#include "logger.h"

namespace ProfilerStackCapture
{

/// <summary>
/// Platform-agnostic interface for capturing thread stacks.
/// Uses declarative guards and runtime abstraction for clarity and maintainability.
/// - .NET Core/5+: CLR suspension (global, all threads blocked)
/// - .NET Framework: OS thread suspension (per-thread granularity)
/// </summary>
class IStackCapturer
{
public:
    virtual ~IStackCapturer() = default;

    /// <summary>
    /// Captures stacks for specified threads via seedless DoStackSnapshot.
    /// And uses RTL based native walk fallback on Windows x64 when DSS fails (e.g. thread in native code with no
    /// managed frames on top).
    /// </summary>
    /// <param name="threads">Set of managed thread IDs to capture</param>
    /// <param name="clientData">Callback context passed to DoStackSnapshotUnseeded</param>
    /// <returns>S_OK on success, error HRESULT otherwise</returns>
    /// 
    /// THREAD SAFETY CONTRACT:
    /// - Caller MUST NOT hold locks that sampled threads might acquire
    /// - Implementation handles ALL suspension/resume logic
    /// - Exception safety: MUST guarantee resume even on errors
    virtual HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads, void* clientData) = 0;

    /// <summary>
    /// Resolve native symbol name (deferred to native walk phase).
    /// </summary>
    virtual HRESULT ResolveNativeSymbolName(UINT_PTR instructionPointer, trace::WSTRING& outName)
    {
        (void)instructionPointer;
        (void)outName;
        return S_FALSE;  // Not available in seedless phase
    }

    // Thread lifecycle notifications (for future NetFx canary coordination)
    virtual void OnThreadCreated(ThreadID threadId) {}
    virtual void OnThreadDestroyed(ThreadID threadId) {}
    virtual void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) {}
    virtual void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) {}
};

/// <summary>
/// Factory function: creates stack capturer for specified runtime type.
/// PIMPL pattern - implementation details (guards, IRuntime, etc.) hidden in .cpp
/// </summary>
/// <param name="profilerInfo">CLR profiler API</param>
/// <param name="runtimeType">Runtime being profiled (.NET Core or .NET Framework)</param>
/// <returns>Heap-allocated capturer (caller owns), or nullptr on error</returns>
std::unique_ptr<IStackCapturer> CreateStackCapturer(
    ICorProfilerInfo2*  profilerInfo,
    continuous_profiler::RuntimeType runtimeType);

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_STACK_CAPTURER_H_
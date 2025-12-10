// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include <corhlpr.h>
#include <corprof.h>
#include <unordered_set>
namespace continuous_profiler {

/// <summary>
/// Platform-agnostic interface for capturing thread stacks.
/// Different implementations handle platform-specific suspension mechanisms:
/// - .NET Core/5+: CLR suspension (SuspendRuntime/ResumeRuntime)
/// - .NET Framework: Per-thread suspension + seeded DoStackSnapshot
/// </summary>
struct StackSnapshotCallbackContext;
using StackFrameCallback = std::function<HRESULT(StackSnapshotCallbackContext* clientData)>;

struct StackSnapshotCallbackContext
{
    StackFrameCallback       callback;
    FunctionID               functionId         = 0;
    UINT_PTR                 instructionPointer = 0;
    COR_PRF_FRAME_INFO       frameInfo          = 0;
    ULONG32                  contextSize        = 0;
    BYTE*                    context            = nullptr;
    ThreadID                 threadId           = 0;
};
    class IStackCaptureStrategy {
public:
    virtual ~IStackCaptureStrategy() = default;

    /// <summary>
    /// Captures stacks for specified threads.
    /// </summary>
    /// <param name="threads">Set of managed thread IDs to capture stacks for</param>
    /// <param name="callback">Callback function invoked for each stack frame</param>
    /// <param name="clientData">Opaque client data passed to callback</param>
    /// <returns>S_OK on success, error HRESULT otherwise</returns>
    /// 
    /// THREAD SAFETY CONTRACT:
    /// - Caller MUST NOT hold locks that sampled threads might acquire
    /// - Caller SHOULD hold profiling_lock to serialize with AllocationTick
    /// 
    /// IMPLEMENTATION NOTES:
    /// - .NET Core: Suspends entire CLR (all app threads frozen)
    /// - .NET Framework: Suspends only target threads (per-thread granularity)
    /// 
    /// SUSPENSION OWNERSHIP:
    /// - Implementation is responsible for ALL suspension/resume logic
    /// - Caller should NOT call SuspendRuntime/ResumeRuntime
    /// - Exception safety: Implementation MUST guarantee resume even on errors
    virtual HRESULT CaptureStacks(
        const std::unordered_set<ThreadID>& threads,
                                  StackSnapshotCallbackContext*       clientData) = 0;
    
    // Optional lifecycle hooks (default no-op implementations)
    // Only .NET Framework strategy needs these for canary thread tracking
    virtual void OnThreadCreated(ThreadID threadId) {}
    virtual void OnThreadDestroyed(ThreadID threadId) {}
    virtual void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) {}
    virtual void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) {}

    static HRESULT __stdcall StackSnapshotCallbackDefault(
        FunctionID funcId,
        UINT_PTR ip,
        COR_PRF_FRAME_INFO frameInfo,
        ULONG32 contextSize,
        BYTE context[],
        void* clientData)
    {
        auto* callbackData               = static_cast<StackSnapshotCallbackContext*>(clientData);
        callbackData->functionId   = funcId;
        callbackData->instructionPointer = ip;
        callbackData->frameInfo          = frameInfo;
        callbackData->contextSize        = contextSize;
        callbackData->context            = context;

        return callbackData->callback(callbackData);
    }
};

} // namespace continuous_profiler

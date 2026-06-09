// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_API_H_
#define OTEL_PROFILER_API_H_

#include <memory>
#include <corhlpr.h>
#include <corprof.h>
#include <functional>
#include "stack_capture_types.h"

namespace ProfilerStackCapture
{
struct StackSnapshotCallbackContext;
using StackFrameCallback = std::function<HRESULT(StackSnapshotCallbackContext* clientData)>;

/// @brief Strategy-internal callback context.
/// Embeds the public CapturedFrame so the bridge in StackWalkerImpl can
/// pass a pointer to the embedded frame without copying any fields.
struct StackSnapshotCallbackContext
{
    StackFrameCallback                   callback;
    continuous_profiler::CapturedFrame   frame;
};

inline HRESULT __stdcall StackSnapshotCallbackDefault(
    FunctionID funcId, UINT_PTR ip, COR_PRF_FRAME_INFO frameInfo, ULONG32 contextSize, BYTE context[], void* clientData)
{
    auto* callbackData                     = static_cast<StackSnapshotCallbackContext*>(clientData);
    callbackData->frame.functionId         = funcId;
    callbackData->frame.instructionPointer = ip;
    callbackData->frame.frameInfo          = frameInfo;
    callbackData->frame.contextSize        = contextSize;
    callbackData->frame.context            = context;
    callbackData->frame.isUnmanagedFrame   = funcId == 0; // CLR signals native frames with funcId == 0 and non-null IP

    return callbackData->callback(callbackData);
}

class IProfilerApi
{
public:
    virtual ~IProfilerApi()                            = default;
    virtual HRESULT DoStackSnapshot(ThreadID              threadId,
                                    StackSnapshotCallback callback,
                                    DWORD                 infoFlags,
                                    void*                 clientData,
                                    BYTE*                 context,
                                    ULONG                 contextSize) = 0;

    HRESULT DoStackSnapshotUnseeded(ThreadID threadId, StackSnapshotCallbackContext* clientData)
    {
        clientData->frame.threadId = threadId;

        return DoStackSnapshot(threadId, StackSnapshotCallbackDefault,
                               COR_PRF_SNAPSHOT_DEFAULT, clientData, nullptr, 0);
    }

    virtual HRESULT GetThreadInfo(ThreadID managedThreadId, DWORD* osThreadId) = 0;
    virtual HRESULT GetFunctionFromIP(LPCBYTE ip, FunctionID* pFunctionId)     = 0;
    virtual HRESULT SuspendRuntime()  { return E_NOTIMPL; }
    virtual HRESULT ResumeRuntime()   { return E_NOTIMPL; }
};

class ProfilerApiAdapter : public IProfilerApi
{
public:
    explicit ProfilerApiAdapter(ICorProfilerInfo2* profilerInfo);
    ~ProfilerApiAdapter() override;

    ProfilerApiAdapter(const ProfilerApiAdapter&)            = delete;
    ProfilerApiAdapter& operator=(const ProfilerApiAdapter&) = delete;

    HRESULT DoStackSnapshot(ThreadID threadId, StackSnapshotCallback callback,
                            DWORD infoFlags, void* clientData,
                            BYTE* context, ULONG contextSize) override;
    HRESULT GetThreadInfo(ThreadID managedThreadId, DWORD* osThreadId) override;
    HRESULT GetFunctionFromIP(LPCBYTE ip, FunctionID* pFunctionId) override;
    HRESULT SuspendRuntime() override;
    HRESULT ResumeRuntime() override;

private:
    struct Impl;
    std::unique_ptr<Impl> pImpl_;
};

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_API_H_
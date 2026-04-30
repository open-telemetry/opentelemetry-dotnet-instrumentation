// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_API_H_
#define OTEL_PROFILER_API_H_

//#include "stack_capture_strategy.h"
#include <memory>
#include <corhlpr.h>
#include <corprof.h>
#include <functional>

namespace ProfilerStackCapture
{
struct StackSnapshotCallbackContext;
using StackFrameCallback = std::function<HRESULT(StackSnapshotCallbackContext* clientData)>;

struct StackSnapshotCallbackContext
{
    StackFrameCallback callback;
    FunctionID         functionId         = 0;
    UINT_PTR           instructionPointer = 0;
    COR_PRF_FRAME_INFO frameInfo          = 0;
    ULONG32            contextSize        = 0;
    BYTE*              context            = nullptr;
    ThreadID           threadId           = 0;
    bool               isNativeWalkFrame  = false; // Set by RTL walker only
};

inline HRESULT __stdcall StackSnapshotCallbackDefault(
    FunctionID funcId, UINT_PTR ip, COR_PRF_FRAME_INFO frameInfo, ULONG32 contextSize, BYTE context[], void* clientData)
{
    auto* callbackData               = static_cast<StackSnapshotCallbackContext*>(clientData);
    callbackData->functionId         = funcId;
    callbackData->instructionPointer = ip;
    callbackData->frameInfo          = frameInfo;
    callbackData->contextSize        = contextSize;
    callbackData->context            = context;
    callbackData->isNativeWalkFrame  = false; // DSS frames are never meaningful native frames

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

    HRESULT DoStackSnapshotUnseeded(ThreadID threadId, void* clientData)
    {
        return DoStackSnapshot(threadId, StackSnapshotCallbackDefault,
                               COR_PRF_SNAPSHOT_DEFAULT, clientData, nullptr, 0);
    }

    virtual HRESULT GetThreadInfo(ThreadID managedThreadId, DWORD* osThreadId) = 0;
    virtual HRESULT SuspendRuntime()  { return E_NOTIMPL; }
    virtual HRESULT ResumeRuntime()   { return E_NOTIMPL; }
};

/// @brief Adapts ICorProfilerInfo2/10 to IProfilerApi using PIMPL.
///
/// Cross-platform. COM details (ICorProfilerInfo10 QI, Release) are hidden
/// in profiler_api_adapter.cpp. On .NET Framework, SuspendRuntime/ResumeRuntime
/// gracefully return E_NOTIMPL since ICorProfilerInfo10 is unavailable.
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
    HRESULT SuspendRuntime() override;
    HRESULT ResumeRuntime() override;

private:
    struct Impl;
    std::unique_ptr<Impl> pImpl_;
};

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_API_H_
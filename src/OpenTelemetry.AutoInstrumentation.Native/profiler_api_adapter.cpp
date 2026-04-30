// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "profiler_api.h"
#include <corhlpr.h>
#include <corprof.h>

namespace ProfilerStackCapture
{

// -- PIMPL implementation - all COM details hidden here -----------------------

struct ProfilerApiAdapter::Impl
{
    ICorProfilerInfo2*  profilerInfo;
    ICorProfilerInfo10* profilerInfo10 = nullptr;

    explicit Impl(ICorProfilerInfo2* info) : profilerInfo(info)
    {
        // QI for ICorProfilerInfo10 (.NET Core 3.0+ / .NET 5+).
        // Fails gracefully on .NET Framework and older runtimes -
        // SuspendRuntime/ResumeRuntime will return E_NOTIMPL.
        if (profilerInfo)
        {
            profilerInfo->QueryInterface(__uuidof(ICorProfilerInfo10), reinterpret_cast<void**>(&profilerInfo10));
        }
    }

    ~Impl()
    {
        if (profilerInfo10)
        {
            profilerInfo10->Release();
            profilerInfo10 = nullptr;
        }
    }

    Impl(const Impl&)            = delete;
    Impl& operator=(const Impl&) = delete;
};

// -- Public API delegates to Impl ---------------------------------------------

ProfilerApiAdapter::ProfilerApiAdapter(ICorProfilerInfo2* profilerInfo)
    : pImpl_(std::make_unique<Impl>(profilerInfo))
{
}

ProfilerApiAdapter::~ProfilerApiAdapter() = default;

HRESULT ProfilerApiAdapter::DoStackSnapshot(ThreadID              threadId,
                                            StackSnapshotCallback callback,
                                            DWORD                 infoFlags,
                                            void*                 clientData,
                                            BYTE*                 context,
                                            ULONG                 contextSize)
{
    return pImpl_->profilerInfo->DoStackSnapshot(threadId, callback, infoFlags,
                                                  clientData, context, contextSize);
}

HRESULT ProfilerApiAdapter::GetThreadInfo(ThreadID managedThreadId, DWORD* osThreadId)
{
    return pImpl_->profilerInfo->GetThreadInfo(managedThreadId, osThreadId);
}

HRESULT ProfilerApiAdapter::GetFunctionFromIP(LPCBYTE ip, FunctionID* pFunctionId)
{
    if (!pImpl_->profilerInfo || !ip || !pFunctionId)
        return E_INVALIDARG;
    return pImpl_->profilerInfo->GetFunctionFromIP(ip, pFunctionId);
}

HRESULT ProfilerApiAdapter::SuspendRuntime()
{
    if (!pImpl_->profilerInfo10)
        return E_NOTIMPL;
    return pImpl_->profilerInfo10->SuspendRuntime();
}

HRESULT ProfilerApiAdapter::ResumeRuntime()
{
    if (!pImpl_->profilerInfo10)
        return E_NOTIMPL;
    return pImpl_->profilerInfo10->ResumeRuntime();
}

} // namespace ProfilerStackCapture
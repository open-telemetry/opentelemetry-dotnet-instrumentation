// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32)

#include "stack_safety_probe.h"
#include "logger.h"
#include <atomic>

#ifndef DECLSPEC_IMPORT
#define DECLSPEC_IMPORT __declspec(dllimport)
#endif

#if defined(_M_AMD64)
extern "C"
{
    DECLSPEC_IMPORT PRUNTIME_FUNCTION NTAPI RtlLookupFunctionEntry(DWORD64,
                                                                   PDWORD64,
                                                                   PUNWIND_HISTORY_TABLE);
}
#endif

namespace ProfilerStackCapture
{

// SEH cannot coexist with C++ object unwinding in the same function.
static HRESULT ExecuteProbeOperationsSEH(ProbeFlags flags,
                                         ThreadID   canaryManagedId,
                                         DWORD64    probeRip,
                                         IProfilerApi* profilerApi)
{
    HRESULT result    = S_OK;
    int*    testAlloc = nullptr;

    __try
    {
        if (HasFlag(flags, ProbeFlags::HeapAlloc))
        {
            testAlloc = static_cast<int*>(std::malloc(sizeof(int)));
            if (!testAlloc)
                return E_OUTOFMEMORY;
            *testAlloc = 42;
            std::free(testAlloc);
            testAlloc = nullptr;
        }

#if defined(_M_AMD64)
        if (HasFlag(flags, ProbeFlags::RtlUnwind))
        {
            UNWIND_HISTORY_TABLE historyTable = {};
            DWORD64              imageBase    = 0;
            RtlLookupFunctionEntry(probeRip, &imageBase, &historyTable);
        }
#endif

        if (HasFlag(flags, ProbeFlags::DoStackSnapshot))
        {
            auto cb = [](FunctionID, UINT_PTR, COR_PRF_FRAME_INFO, ULONG32, BYTE[], void*) -> HRESULT
            { return S_FALSE; };
            result = profilerApi->DoStackSnapshot(canaryManagedId, cb, COR_PRF_SNAPSHOT_DEFAULT,
                                                  nullptr, nullptr, 0);
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        DWORD exCode = GetExceptionCode();
        trace::Logger::Debug("[StackSafetyProbe] SEH exception in probe worker. Code=0x",
                             std::hex, exCode, std::dec);
        if (testAlloc) std::free(testAlloc);
        return E_FAIL;
    }

    if (HasFlag(flags, ProbeFlags::DoStackSnapshot) && result == CORPROF_E_STACKSNAPSHOT_ABORTED)
        result = S_OK;
    return result;
}

StackSafetyProbe::StackSafetyProbe(IProfilerApi*             profilerApi,
                                   InvocationQueue*          queue,
                                   ProbeFlags                flags,
                                   std::chrono::milliseconds probeTimeout)
    : profilerApi_(profilerApi), queue_(queue), flags_(flags), probeTimeout_(probeTimeout)
{
}

bool StackSafetyProbe::Run(ThreadID canaryManagedId, DWORD64 probeRip)
{
    if (flags_ == ProbeFlags::None)
        return true;
    if (queue_ == nullptr)
    {
        trace::Logger::Error("[StackSafetyProbe] No InvocationQueue provided for probe execution.");
        return false;
    }
    auto probeHr     = std::make_shared<std::atomic<HRESULT>>(S_OK);
    auto profilerApi = profilerApi_;
    auto flags       = flags_;

    auto status = queue_->Invoke(
        [flags, canaryManagedId, probeRip, profilerApi, probeHr]()
        {
            probeHr->store(ExecuteProbeOperationsSEH(flags, canaryManagedId, probeRip, profilerApi));
        },
        probeTimeout_);

    if (status != InvocationStatus::Invoked)
    {
        trace::Logger::Warn("[StackSafetyProbe] Probe timed out after ", probeTimeout_.count(), "ms");
        return false;
    }

    HRESULT hr = probeHr->load();
    if (hr == CORPROF_E_STACKSNAPSHOT_UNSAFE)
    {
        trace::Logger::Warn("[StackSafetyProbe] DoStackSnapshot returned CORPROF_E_STACKSNAPSHOT_UNSAFE");
        return false;
    }
    if (FAILED(hr))
    {
        trace::Logger::Error("[StackSafetyProbe] Probe failed. HRESULT=0x", std::hex, hr, std::dec);
        return false;
    }
    return true;
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
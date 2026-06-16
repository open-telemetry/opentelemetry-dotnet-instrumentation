// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && defined(_M_AMD64)

#include "safe_native_walk_service.h"
#include "native_symbol_resolver_impl.h"
#include "thread_suspend.h"
#include "logger.h"

namespace ProfilerStackCapture
{

SafeNativeWalkService::SafeNativeWalkService(IProfilerApi* profilerApi) : profilerApi_(profilerApi) {}

SafeNativeWalkService::~SafeNativeWalkService() {}

INativeSymbolResolver& SafeNativeWalkService::GetSymbolResolver()
{
    return NativeSymbolResolver::Instance();
}

HRESULT SafeNativeWalkService::ContinueFromProbedFrame0(ThreadGuard&                              threadGuard,
                                                        ThreadID                                  managedThreadId,
                                                        const CONTEXT&                            ctx,
                                                        const continuous_profiler::CapturedFrame& frame0,
                                                        StackSnapshotCallbackContext*             clientData)
{
    // ThreadGuard kept in the signature as a compile-time suspension
    // contract. The body relies on the target remaining suspended for
    // the full duration via the caller's RAII scope.
    (void)threadGuard;

    // Managed frame-0: ctx is the seed (probe left it unchanged - it did
    // NOT unwind in the managed branch). Hand straight to seeded DSS;
    // skip the native walk entirely.
    if (!frame0.isUnmanagedFrame)
    {
        clientData->frame.threadId = managedThreadId;

        trace::Logger::Debug("[SafeNativeWalkService] Frame-0 managed; seeded DSS. ManagedID=", managedThreadId);

        return IssueSeededDss(managedThreadId, ctx, clientData);
    }

    // Native frame-0: emit composed frame, then continue the native walk
    // from frame-1 on the unwound ctx. The wholesale copy overwrites
    // threadId with the probe's default-initialized zero, so restamp it
    // AFTER the copy. The continuation will hit a managed boundary
    // internally and switch to seeded DSS.
    trace::Logger::Debug("[SafeNativeWalkService] Frame-0 native; emit + continue. ManagedID=", managedThreadId);

    clientData->frame          = frame0;
    clientData->frame.threadId = managedThreadId;

    HRESULT cbHr = clientData->callback(clientData);
    if (cbHr == S_FALSE)
        return S_OK; // caller requested early stop after frame-0
    if (FAILED(cbHr))
        return cbHr;

    NativeWalkResult walkResult = WalkNativeUntilManaged(ctx, clientData);

    if (FAILED(walkResult.hr) && walkResult.nativeFrameCount == 0)
        return walkResult.hr;

    // Seed DSS from the managed boundary the native walk landed on -
    // managed frames are then captured with CLR accuracy.
    HRESULT hr = walkResult.hr;
    if (walkResult.hasSeed)
    {
        hr = IssueSeededDss(managedThreadId, walkResult.seedCtx, clientData);
    }

    trace::Logger::Debug("[SafeNativeWalkService] Capture complete. Native frames=", walkResult.nativeFrameCount,
                         ", DSS seed=", walkResult.hasSeed ? "yes" : "no", ", HRESULT=", trace::HResultStr(hr));
    return hr;
}

HRESULT SafeNativeWalkService::IssueSeededDss(ThreadID                      managedThreadId,
                                              const CONTEXT&                seedCtx,
                                              StackSnapshotCallbackContext* clientData)
{
    // DoStackSnapshot's seedContext is typed BYTE* (non-const) but is
    // documented and observed as read-only; const_cast is safe.
    HRESULT hr = profilerApi_->DoStackSnapshot(managedThreadId, StackSnapshotCallbackDefault, COR_PRF_SNAPSHOT_DEFAULT,
                                               clientData, reinterpret_cast<BYTE*>(const_cast<CONTEXT*>(&seedCtx)),
                                               sizeof(CONTEXT));

    // CORPROF_E_STACKSNAPSHOT_ABORTED == callback returned S_FALSE to
    // terminate the walk cleanly; normalize so callers don't surface a
    // user-driven early-stop as a failure.
    if (hr == CORPROF_E_STACKSNAPSHOT_ABORTED)
        hr = S_OK;
    return hr;
}

NativeWalkResult SafeNativeWalkService::WalkNativeUntilManaged(const CONTEXT&                initialCtx,
                                                               StackSnapshotCallbackContext* clientData)
{
    NativeWalkResult result;
    result.hr                         = E_FAIL;
    CONTEXT              threadCtx    = initialCtx;
    auto&                resolver     = NativeSymbolResolver::Instance();
    DWORD                frameCount   = 0;
    constexpr DWORD      kMaxDepth    = 512;
    UNWIND_HISTORY_TABLE historyTable = {};
    __try
    {
        while (threadCtx.Rip != 0 && frameCount < kMaxDepth)
        {
            // Check if this IP belongs to a managed (JIT-compiled) function
            FunctionID managedFuncId = 0;
            HRESULT    fnHr = profilerApi_->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(threadCtx.Rip), &managedFuncId);

            if (SUCCEEDED(fnHr) && managedFuncId != 0)
            {
                // Hit managed code - capture seed context and stop native walk.
                // DSS seeded with this context will continue from here.
                result.hasSeed = true;
                result.seedCtx = threadCtx;
                result.hr      = S_OK;
                break;
            }

            // Native frame - emit it via callback
            DWORD64           imageBase   = 0;
            DWORD64           previousRip = threadCtx.Rip;
            PRUNTIME_FUNCTION rtFunc      = RtlLookupFunctionEntry(threadCtx.Rip, &imageBase, &historyTable);

            UINT_PTR frameIp;
            if (imageBase != 0)
            {
                // nullopt (GetModuleFileNameW failed) and true (system module)
                // both fall to the BeginAddress/Rip branch, matching the
                // pre-SRW behavior where GetModuleInfo returned nullptr or
                // a system-classified entry.
                auto sys = resolver.IsSystemModule(static_cast<UINT_PTR>(imageBase));
                if (sys.has_value() && !*sys)
                {
                    frameIp = static_cast<UINT_PTR>(imageBase);
                }
                else
                {
                    frameIp = (rtFunc != nullptr) ? static_cast<UINT_PTR>(imageBase + rtFunc->BeginAddress)
                                                  : static_cast<UINT_PTR>(threadCtx.Rip);
                }
            }
            else
            {
                frameIp = static_cast<UINT_PTR>(threadCtx.Rip);
            }

            clientData->frame.functionId         = 0;
            clientData->frame.instructionPointer = frameIp;
            clientData->frame.frameInfo          = 0;
            clientData->frame.contextSize        = 0;
            clientData->frame.context            = nullptr;
            clientData->frame.isUnmanagedFrame   = true;

            HRESULT cbResult = clientData->callback(clientData);
            if (cbResult != S_OK)
            {
                // S_FALSE: caller requested early stop - clean termination
                // E_*: real failure - propagate to caller
                result.hr = (cbResult == S_FALSE) ? S_OK : cbResult;
                break;
            }

            ++frameCount;

            // Unwind one frame
            if (rtFunc == nullptr)
            {
                // Leaf function - RSP points directly at return address
                if (threadCtx.Rsp == 0)
                    break;
                threadCtx.Rip = *reinterpret_cast<const DWORD64*>(threadCtx.Rsp);
                threadCtx.Rsp += 8;
            }
            else
            {
                void*   handlerData      = nullptr;
                DWORD64 establisherFrame = 0;
                RtlVirtualUnwind(UNW_FLAG_NHANDLER, imageBase, threadCtx.Rip, rtFunc, &threadCtx, &handlerData,
                                 &establisherFrame, nullptr);
            }

            // Guard against corrupt unwind data
            if (threadCtx.Rip == previousRip)
                break;
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        DWORD exCode = GetExceptionCode();
        trace::Logger::Debug("[SafeNativeWalkService] Exception during native walk. Code=0x", std::hex, exCode,
                             std::dec, ", Frames=", frameCount);
        result.hr = (frameCount > 0) ? S_OK : E_FAIL;
    }

    result.nativeFrameCount = frameCount;
    if (!result.hasSeed && frameCount > 0)
        result.hr = S_OK;

    return result;
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32) && defined(_M_AMD64)
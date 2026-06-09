// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && defined(_M_AMD64)

#include "safe_native_walk_service.h"
#include "rtl_stack_walk.h"
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

HRESULT SafeNativeWalkService::CaptureNativeThenSeededDss(ThreadGuard&                  threadGuard,
                                                          ThreadID                      managedThreadId,
                                                          StackSnapshotCallbackContext* clientData)
{
    // ThreadGuard is the suspension contract by construction - an intentional
    // ergonomics choice: the signature itself rejects unsuspended threads at
    // compile time, no documentation or convention required.
    CONTEXT ctx      = {};
    ctx.ContextFlags = CONTEXT_FULL;
    if (!threadGuard.GetContext(ctx))
    {
        trace::Logger::Debug("[SafeNativeWalkService] GetThreadContext failed. Error=", GetLastError());
        return E_FAIL;
    }

    // Stage 1: RTL walk - native frames until managed boundary
    NativeWalkResult walkResult = WalkNativeUntilManaged(ctx, clientData);

    if (FAILED(walkResult.hr) && walkResult.nativeFrameCount == 0)
        return walkResult.hr;

    // Stage 2: DSS seeded from boundary - managed frames with CLR accuracy
    HRESULT hr = walkResult.hr;
    if (walkResult.hasSeed)
    {
        hr = profilerApi_->DoStackSnapshot(managedThreadId, StackSnapshotCallbackDefault, COR_PRF_SNAPSHOT_DEFAULT,
                                           clientData, reinterpret_cast<BYTE*>(&walkResult.seedCtx),
                                           sizeof(walkResult.seedCtx));

        if (hr == CORPROF_E_STACKSNAPSHOT_ABORTED)
            hr = S_OK;
    }

    trace::Logger::Debug("[SafeNativeWalkService] Capture complete. Native frames=", walkResult.nativeFrameCount,
                         ", DSS seed=", walkResult.hasSeed ? "yes" : "no", ", HRESULT=", trace::HResultStr(hr));
    return hr;
}

NativeWalkResult SafeNativeWalkService::WalkNativeUntilManaged(const CONTEXT&                initialCtx,
                                                               StackSnapshotCallbackContext* clientData)
{
    NativeWalkResult result;
    result.hr                  = E_FAIL;
    CONTEXT         threadCtx  = initialCtx;
    auto&           resolver   = NativeSymbolResolver::Instance();
    DWORD           frameCount = 0;
    constexpr DWORD kMaxDepth  = 512;

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
            DWORD64              imageBase    = 0;
            UNWIND_HISTORY_TABLE historyTable = {};
            DWORD64              previousRip  = threadCtx.Rip;
            PRUNTIME_FUNCTION    rtFunc       = RtlLookupFunctionEntry(threadCtx.Rip, &imageBase, &historyTable);

            UINT_PTR frameIp;
            if (imageBase != 0)
            {
                const auto* mod = resolver.GetModuleInfo(static_cast<UINT_PTR>(imageBase));
                if (mod != nullptr && !mod->isSystem)
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
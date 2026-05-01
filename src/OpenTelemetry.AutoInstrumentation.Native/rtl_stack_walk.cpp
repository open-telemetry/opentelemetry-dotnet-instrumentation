// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && defined(_M_AMD64)

#include "rtl_stack_walk.h"
#include "thread_suspend.h" // ScopedThreadSuspend full definition
#include "logger.h"
#include <algorithm>

namespace ProfilerStackCapture
{

// ============================================================================
// WalkNativeStack
// ============================================================================

HRESULT WalkNativeStack(void* suspendedThread, NativeWalkContext* ctx)
{
    if (!suspendedThread || !ctx || !ctx->clientParams)
        return E_INVALIDARG;

    CONTEXT threadCtx      = {};
    threadCtx.ContextFlags = CONTEXT_FULL;
    if (!GetThreadContext(suspendedThread, &threadCtx))
    {
        DWORD err = GetLastError();
        trace::Logger::Debug("[RtlStackWalk] GetThreadContext failed. Error=", err);
        return E_FAIL;
    }

    auto*   clientParams = ctx->clientParams;
    DWORD   frameCount   = 0;
    HRESULT cbResult     = S_OK;

    // Access the singleton to classify modules during the walk.
    auto& resolver = NativeSymbolResolver::Instance();

    __try
    {
        while (threadCtx.Rip != 0 && frameCount < ctx->maxDepth)
        {
            if (ctx->stopRequested && ctx->stopRequested->load())
                break;

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
                    // Non-system DLL: collapse all frames to imageBase.
                    // The per-function offset has no value - we only report
                    // "modulename!0xIP" for these anyway.
                    frameIp = static_cast<UINT_PTR>(imageBase);
                }
                else
                {
                    // System DLL or unknown module: keep function-level granularity
                    // so export table RVA walk can resolve distinct symbols.
                    frameIp = (rtFunc != nullptr) ? static_cast<UINT_PTR>(imageBase + rtFunc->BeginAddress)
                                                  : static_cast<UINT_PTR>(threadCtx.Rip);
                }
            }
            else
            {
                frameIp = static_cast<UINT_PTR>(threadCtx.Rip);
            }

            // Attempt to resolve IP as a JIT-compiled managed function.
            FunctionID managedFuncId  = 0;
            bool       isManagedFrame = false;
            if (ctx->profilerApi != nullptr)
            {
                HRESULT fnHr =
                    ctx->profilerApi->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(threadCtx.Rip), &managedFuncId);
                if (SUCCEEDED(fnHr) && managedFuncId != 0)
                {
                    isManagedFrame = true;
                }
            }

            // Emit frame - if managed, pass the FunctionID so caller can resolve it
            // via the normal managed name resolution path.
            clientParams->functionId         = isManagedFrame ? managedFuncId : 0;
            clientParams->instructionPointer = isManagedFrame ? static_cast<UINT_PTR>(threadCtx.Rip) : frameIp;
            clientParams->frameInfo          = 0;
            clientParams->contextSize        = 0;
            clientParams->context            = nullptr;
            clientParams->isNativeWalkFrame  = !isManagedFrame;

            cbResult = clientParams->callback(clientParams);
            if (cbResult != S_OK)
                break;

            ++frameCount;

            if (rtFunc == nullptr)
            {
                // Leaf function - RSP points directly at the return address.
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

            // Guard against corrupt unwind data that fails to advance the IP.
            if (threadCtx.Rip == previousRip)
                break;
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        DWORD exCode = GetExceptionCode();
        trace::Logger::Debug("[RtlStackWalk] Exception during walk. ExceptionCode=0x", std::hex, exCode, std::dec,
                             ", FrameCount=", std::dec, frameCount);
        return frameCount > 0 ? S_OK : E_FAIL;
    }

    if (cbResult == S_FALSE || cbResult == CORPROF_E_STACKSNAPSHOT_ABORTED)
        return S_OK;

    return frameCount > 0 ? S_OK : E_FAIL;
}

// ============================================================================
// NativeSymbolResolver
// ============================================================================

NativeSymbolResolver& NativeSymbolResolver::Instance()
{
    static NativeSymbolResolver instance;
    return instance;
}

NativeSymbolResolver::NativeSymbolResolver()
{
    WCHAR sysDir[MAX_PATH] = {};
    if (GetSystemDirectory(sysDir, MAX_PATH) > 0)
    {
        sysDir_ = sysDir;
        std::transform(sysDir_.begin(), sysDir_.end(), sysDir_.begin(), ::towlower);
    }
}

NativeSymbolResolver::~NativeSymbolResolver() = default;

// ----------------------------------------------------------------------------
// Module info cache
// ----------------------------------------------------------------------------

const NativeSymbolResolver::ModuleInfo* NativeSymbolResolver::GetOrCreateModuleInfo(UINT_PTR imageBase)
{
    auto it = moduleCache_.find(imageBase);
    if (it != moduleCache_.end())
        return &it->second;

    // Evict entire cache if it grows too large (simple reset - these are cheap to rebuild).
    if (moduleCache_.size() >= kMaxModuleCacheSize)
    {
        trace::Logger::Debug("[NativeSymbolResolver] Module cache full, clearing ", moduleCache_.size(), " entries");
        moduleCache_.clear();
    }

    WCHAR modulePath[MAX_PATH] = {};
    if (GetModuleFileNameW(reinterpret_cast<HMODULE>(imageBase), modulePath, MAX_PATH) == 0)
        return nullptr;

    ModuleInfo info;

    // Extract base name (e.g. "ntdll.dll" from "C:\Windows\System32\ntdll.dll").
    const WCHAR* slash = wcsrchr(modulePath, L'\\');
    info.baseName      = slash ? (slash + 1) : modulePath;

    // Check if module resides under the system directory.
    info.isSystem = false;
    if (!sysDir_.empty())
    {
        std::wstring pathLower(modulePath);
        std::transform(pathLower.begin(), pathLower.end(), pathLower.begin(), ::towlower);
        info.isSystem = (pathLower.find(sysDir_) == 0);
    }

    auto [inserted, success] = moduleCache_.emplace(imageBase, std::move(info));
    return &inserted->second;
}

const NativeSymbolResolver::ModuleInfo* NativeSymbolResolver::GetModuleInfo(UINT_PTR imageBase)
{
    return GetOrCreateModuleInfo(imageBase);
}
// ----------------------------------------------------------------------------
// Private helpers
// ----------------------------------------------------------------------------

void NativeSymbolResolver::AppendNarrow(trace::WSTRING& out, const char* s, size_t sLen)
{
    if (s == nullptr || sLen == 0)
        return;

    const auto* u = reinterpret_cast<const unsigned char*>(s);
    std::transform(u, u + sLen, std::back_inserter(out), [](unsigned char c) { return static_cast<WCHAR>(c); });
}

size_t NativeSymbolResolver::FindExportNameForRva(UINT_PTR imageBase, DWORD rva, char* nameBuf, size_t nameBufSize)
{
    nameBuf[0] = '\0';
    if (nameBufSize < 5)
        return 0;

    const size_t maxChars = nameBufSize - 1;

    __try
    {
        auto* dos = reinterpret_cast<PIMAGE_DOS_HEADER>(imageBase);
        if (dos->e_magic != IMAGE_DOS_SIGNATURE)
            return 0;

        const LONG lfanew = dos->e_lfanew;
        if (lfanew < static_cast<LONG>(sizeof(IMAGE_DOS_HEADER)) || lfanew > 0x10000000)
            return 0;

        auto* nt = reinterpret_cast<PIMAGE_NT_HEADERS>(imageBase + lfanew);
        if (nt->Signature != IMAGE_NT_SIGNATURE)
            return 0;

        if (nt->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_EXPORT)
            return 0;

        const auto& dir = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
        if (dir.VirtualAddress == 0 || dir.Size == 0)
            return 0;

        const DWORD exportStart = dir.VirtualAddress;
        const DWORD exportEnd   = exportStart + dir.Size;

        auto* exp       = reinterpret_cast<PIMAGE_EXPORT_DIRECTORY>(imageBase + dir.VirtualAddress);
        auto* functions = reinterpret_cast<const DWORD*>(imageBase + exp->AddressOfFunctions);
        auto* names     = reinterpret_cast<const DWORD*>(imageBase + exp->AddressOfNames);
        auto* ordinals  = reinterpret_cast<const WORD*>(imageBase + exp->AddressOfNameOrdinals);

        DWORD       bestRva  = 0;
        const char* bestName = nullptr;

        for (DWORD i = 0; i < exp->NumberOfNames; ++i)
        {
            if (ordinals[i] >= exp->NumberOfFunctions)
                continue;

            const DWORD funcRva = functions[ordinals[i]];

            if (funcRva >= exportStart && funcRva < exportEnd)
                continue;

            if (funcRva <= rva && funcRva > bestRva)
            {
                bestRva  = funcRva;
                bestName = reinterpret_cast<const char*>(imageBase + names[i]);
            }
        }

        if (bestName != nullptr)
        {
            size_t len      = strnlen(bestName, maxChars + 1);
            bool   truncate = (len > maxChars);
            size_t copyLen  = truncate ? maxChars - 3 : len;
            memcpy(nameBuf, bestName, copyLen);
            if (truncate)
            {
                nameBuf[copyLen]     = '.';
                nameBuf[copyLen + 1] = '.';
                nameBuf[copyLen + 2] = '.';
                copyLen += 3;
            }
            nameBuf[copyLen] = '\0';
            return copyLen;
        }
        return 0;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        nameBuf[0] = '\0';
        return 0;
    }
}

UINT_PTR NativeSymbolResolver::GetImageBaseForIp(UINT_PTR ip)
{
    DWORD64              imageBase    = 0;
    UNWIND_HISTORY_TABLE historyTable = {};
    RtlLookupFunctionEntry(static_cast<DWORD64>(ip), &imageBase, &historyTable);

    if (imageBase == 0)
    {
        HMODULE hMod = nullptr;
        if (GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                               reinterpret_cast<LPCWSTR>(ip), &hMod) &&
            hMod != nullptr)
        {
            imageBase = reinterpret_cast<DWORD64>(hMod);
        }
    }

    return static_cast<UINT_PTR>(imageBase);
}

bool NativeSymbolResolver::ResolveViaExports(UINT_PTR          ip,
                                             UINT_PTR          imageBase,
                                             const ModuleInfo& mod,
                                             trace::WSTRING&   outName)
{
    const DWORD rva = static_cast<DWORD>(ip - imageBase);

    char   exportNameBuf[kMaxSymbolLen + 1] = {};
    size_t exportNameLen = FindExportNameForRva(imageBase, rva, exportNameBuf, sizeof(exportNameBuf));

    if (exportNameLen == 0)
        return false;

    outName.clear();
    outName.reserve(mod.baseName.length() + 1 + exportNameLen);

    if (!mod.baseName.empty())
    {
        outName.append(mod.baseName);
        outName += WStr("!");
    }

    AppendNarrow(outName, exportNameBuf, exportNameLen);
    return true;
}

// ----------------------------------------------------------------------------
// Public entry point
// ----------------------------------------------------------------------------

bool NativeSymbolResolver::Resolve(UINT_PTR ip, trace::WSTRING& outName)
{
    if (ip == 0)
        return false;

    UINT_PTR imageBase = GetImageBaseForIp(ip);
    if (imageBase == 0)
        return false;

    const ModuleInfo* mod = GetOrCreateModuleInfo(imageBase);
    if (mod == nullptr)
        return false;

    if (mod->isSystem)
    {
        // System DLLs: full export symbol resolution via RVA walk.
        return ResolveViaExports(ip, imageBase, *mod, outName);
    }
    else
    {
        // Non-system DLLs: short-circuit - no export table walk.
        // The walk already collapsed these frames to imageBase, so just
        // report the module name.
        outName.clear();
        outName.append(mod->baseName);
        return true;
    }
}

// ============================================================================
// WalkNativeStackForThread
// ============================================================================

HRESULT WalkNativeStackForThread(IProfilerApi*                 profilerApi,
                                 ThreadID                      managedThreadId,
                                 StackSnapshotCallbackContext* clientData)
{
    DWORD   osThreadId = 0;
    HRESULT hr         = profilerApi->GetThreadInfo(managedThreadId, &osThreadId);
    if (FAILED(hr))
        return hr;

    NativeWalkContext   nativeCtx{clientData, nullptr, profilerApi};
    ScopedThreadSuspend suspendedThread(osThreadId);
    return WalkNativeStack(suspendedThread.GetHandle(), &nativeCtx);
}

} // namespace ProfilerStackCapture
#endif // defined(_WIN32) && defined(_M_AMD64)

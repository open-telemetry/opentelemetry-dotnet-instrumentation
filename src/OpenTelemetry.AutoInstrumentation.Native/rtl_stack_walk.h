// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_RTL_STACK_WALK_H_
#define OTEL_RTL_STACK_WALK_H_

#if defined(_WIN32) && defined(_M_AMD64)

#include <atomic>
#include <corhlpr.h>
#include <corprof.h>
#include <unordered_map>
#include "string_utils.h"
#include "thread_suspend.h"
#include "profiler_api.h"

namespace ProfilerStackCapture
{

struct NativeWalkContext
{
    StackSnapshotCallbackContext* clientParams  = nullptr;
    std::atomic<bool>*                                stopRequested = nullptr;
    IProfilerApi*                 profilerApi   = nullptr;
    DWORD                                             maxDepth      = 512;
};

HRESULT WalkNativeStack(void* suspendedThread,
                        NativeWalkContext*         ctx);

HRESULT WalkNativeStackForThread(IProfilerApi*                 profilerApi,
                                 ThreadID                      managedThreadId,
                                 StackSnapshotCallbackContext* clientData);

// Symbol resolver - caches the system directory and per-module metadata to
// avoid repeated kernel calls. System DLLs get full export table RVA walk;
// non-system modules get a modulename fallback.
class NativeSymbolResolver
{
public:
    NativeSymbolResolver();
    ~NativeSymbolResolver();

    static NativeSymbolResolver& Instance();

    NativeSymbolResolver(const NativeSymbolResolver&)            = delete;
    NativeSymbolResolver& operator=(const NativeSymbolResolver&) = delete;
    NativeSymbolResolver(NativeSymbolResolver&&)                 = delete;

    // Single entry point for native symbol resolution.
    bool Resolve(UINT_PTR ip, trace::WSTRING& outName);

    // Cached per-module metadata - looked up once per unique imageBase.
    struct ModuleInfo
    {
        trace::WSTRING baseName;  // e.g. "ntdll.dll"
        bool           isSystem;  // true if module lives under system directory
    };

    // Retrieve (or create) cached module info for a given image base.
    // Used by WalkNativeStack to classify frames during capture.
    const ModuleInfo* GetModuleInfo(UINT_PTR imageBase);
    
private:
    const ModuleInfo* GetOrCreateModuleInfo(UINT_PTR imageBase);

    static UINT_PTR GetImageBaseForIp(UINT_PTR ip);
    static void     AppendNarrow(trace::WSTRING& out, const char* s, size_t sLen);
    static size_t   FindExportNameForRva(UINT_PTR imageBase, DWORD rva, char* nameBuf, size_t nameBufSize);
    bool            ResolveViaExports(UINT_PTR ip, UINT_PTR imageBase, const ModuleInfo& mod, trace::WSTRING& outName);

    trace::WSTRING sysDir_;

    // imageBase -> ModuleInfo, avoids repeated GetModuleFileNameW + lowercase + compare.
    std::unordered_map<UINT_PTR, ModuleInfo> moduleCache_;

    static constexpr size_t kMaxSymbolLen       = 256;
    static constexpr size_t kMaxModuleCacheSize = 256;
};

// Convenience free function - delegates to NativeSymbolResolver singleton.
inline bool ResolveNativeSymbolName(UINT_PTR ip, trace::WSTRING& outName)
{
    return NativeSymbolResolver::Instance().Resolve(ip, outName);
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_RTL_STACK_WALK_H_
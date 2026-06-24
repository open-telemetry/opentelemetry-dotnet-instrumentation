// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_NATIVE_SYMBOL_RESOLVER_IMPL_H_
#define OTEL_NATIVE_SYMBOL_RESOLVER_IMPL_H_

#if defined(_WIN32) && defined(_M_AMD64)

#include <atomic>
#include <corhlpr.h>
#include <corprof.h>
#include <unordered_map>
#include "string_utils.h"
#include "profiler_api.h"
#include "native_symbol_resolver.h"

// THREAD-AFFINITY CONTRACT (relaxed): moduleCache_ is protected by
// moduleCacheLock_ (SRWLOCK). Multiple readers may hold it shared
// concurrently; writers (cache miss materialization, watermark eviction)
// acquire it exclusive. Pointers/iterators into moduleCache_ are valid
// only while the lock is held; public APIs copy the fields they need
// (baseName / isSystem) to caller-owned locals before releasing.
//
// Two threads are expected: the sampler (Resolve during frame rendering)
// and the StackWalkGuard worker (IsSystemModule during the native walk).

namespace ProfilerStackCapture
{

// Symbol resolver - caches the system directory and per-module metadata to
// avoid repeated kernel calls. System DLLs get full export table RVA walk;
// non-system modules get a modulename fallback.
class NativeSymbolResolver : public INativeSymbolResolver
{
public:
    NativeSymbolResolver();
    ~NativeSymbolResolver();

    static NativeSymbolResolver& Instance();

    NativeSymbolResolver(const NativeSymbolResolver&)            = delete;
    NativeSymbolResolver& operator=(const NativeSymbolResolver&) = delete;
    NativeSymbolResolver(NativeSymbolResolver&&)                 = delete;

    // Single entry point for native symbol resolution.
    bool Resolve(UINT_PTR ip, trace::WSTRING& outName) override;

    // Classifies a module image base as system / non-system. Returns:
    //   nullopt  - GetModuleFileNameW failed for this image base (matches
    //              the legacy "GetModuleInfo returned nullptr" outcome;
    //              callers should treat this the same as "system" for
    //              fallback frame-IP selection).
    //   false    - module is not under the system directory.
    //   true     - module is under the system directory.
    //
    // Safe to call concurrently with Resolve(): both go through the same
    // SRW-protected cache.
    std::optional<bool> IsSystemModule(UINT_PTR imageBase) const override;
    std::optional<bool> IsSystemModuleFromPath(UINT_PTR imageBase, const WCHAR* modulePath) const;

private:
    // Cached per-module metadata - looked up once per unique imageBase.
    struct ModuleInfo
    {
        trace::WSTRING baseName;  // e.g. "ntdll.dll"
        bool           isSystem;  // true if module lives under system directory
    };

    // Returns true if classification is available for imageBase (cached or
    // freshly materialized). On true, populates the provided out-params
    // with local copies safe to use after this call returns. Either
    // out-param may be null when the caller does not need that field
    // (e.g. IsSystemModule passes nullptr for baseName to skip the
    // WSTRING copy).
    bool FetchClassification(UINT_PTR        imageBase,
                             trace::WSTRING* outBaseName,
                             bool*           outIsSystem) const;
    bool ClassifyAndCache(UINT_PTR imageBase, const WCHAR* modulePath, trace::WSTRING* outBaseName, bool* outIsSystem) const;
    static UINT_PTR GetImageBaseForIp(UINT_PTR ip);
    static void     AppendNarrow(trace::WSTRING& out, const char* s, size_t sLen);
    static size_t   FindExportNameForRva(UINT_PTR imageBase, DWORD rva, char* nameBuf, size_t nameBufSize);
    bool ResolveViaExports(UINT_PTR              ip,
                           UINT_PTR              imageBase,
                           const trace::WSTRING& baseName,
                           trace::WSTRING&       outName);

    trace::WSTRING sysDir_;

    // imageBase -> ModuleInfo, avoids repeated GetModuleFileNameW + lowercase + compare.
    mutable std::unordered_map<UINT_PTR, ModuleInfo> moduleCache_;
    mutable SRWLOCK                                  moduleCacheLock_ = SRWLOCK_INIT;

    static constexpr size_t kMaxSymbolLen       = 256;
    static constexpr size_t kMaxModuleCacheSize = 256;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_NATIVE_SYMBOL_RESOLVER_IMPL_H_
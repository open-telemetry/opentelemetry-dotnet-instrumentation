// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && defined(_M_AMD64)

#include "native_symbol_resolver_impl.h"
#include "logger.h"
#include <algorithm>

namespace ProfilerStackCapture
{

namespace
{
// RAII guards for SRWLOCK. Local to this TU so we do not pollute the
// header with helpers no other code needs.
class SrwSharedLock
{
public:
    explicit SrwSharedLock(SRWLOCK& lk) noexcept : lk_(lk)
    {
        AcquireSRWLockShared(&lk_);
    }
    ~SrwSharedLock()
    {
        ReleaseSRWLockShared(&lk_);
    }
    SrwSharedLock(const SrwSharedLock&)            = delete;
    SrwSharedLock& operator=(const SrwSharedLock&) = delete;

private:
    SRWLOCK& lk_;
};

class SrwExclusiveLock
{
public:
    explicit SrwExclusiveLock(SRWLOCK& lk) noexcept : lk_(lk)
    {
        AcquireSRWLockExclusive(&lk_);
    }
    ~SrwExclusiveLock()
    {
        ReleaseSRWLockExclusive(&lk_);
    }
    SrwExclusiveLock(const SrwExclusiveLock&)            = delete;
    SrwExclusiveLock& operator=(const SrwExclusiveLock&) = delete;

private:
    SRWLOCK& lk_;
};
} // namespace

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
// Module info cache (SRW-protected, double-checked)
// ----------------------------------------------------------------------------

bool NativeSymbolResolver::FetchClassification(UINT_PTR imageBase, trace::WSTRING* outBaseName, bool* outIsSystem) const
{
    // Phase 1: shared lookup. Hit fast-path stays lock-free for other
    // readers; we release as soon as we have copies of the fields the
    // caller asked for.
    {
        SrwSharedLock lk(moduleCacheLock_);
        auto          it = moduleCache_.find(imageBase);
        if (it != moduleCache_.end())
        {
            if (outBaseName != nullptr)
                *outBaseName = it->second.baseName;
            if (outIsSystem != nullptr)
                *outIsSystem = it->second.isSystem;
            return true;
        }
    }

    // Phase 2: cache miss. Do the I/O OUTSIDE the lock so a stuck loader
    // CS on the target thread stalls only this single caller (typically
    // the StackWalkGuard worker, which the sampler drives via abandon)
    // and never blocks other readers.
    WCHAR modulePath[MAX_PATH] = {};
    if (GetModuleFileNameW(reinterpret_cast<HMODULE>(imageBase), modulePath, MAX_PATH) == 0)
    {
        return false;
    }

    ModuleInfo   info;
    const WCHAR* slash = wcsrchr(modulePath, L'\\');
    info.baseName      = slash ? (slash + 1) : modulePath;

    info.isSystem = false;
    if (!sysDir_.empty())
    {
        std::wstring pathLower(modulePath);
        std::transform(pathLower.begin(), pathLower.end(), pathLower.begin(), ::towlower);
        info.isSystem = (pathLower.find(sysDir_) == 0);
    }

    // Phase 3: exclusive emplace with re-check. Another writer may have
    // raced ahead while we were in GetModuleFileNameW; prefer the
    // already-cached entry to keep consistency.
    {
        SrwExclusiveLock lk(moduleCacheLock_);
        if (auto it = moduleCache_.find(imageBase); it != moduleCache_.end())
        {
            if (outBaseName != nullptr)
                *outBaseName = it->second.baseName;
            if (outIsSystem != nullptr)
                *outIsSystem = it->second.isSystem;
            return true;
        }

        if (moduleCache_.size() >= kMaxModuleCacheSize)
        {
            trace::Logger::Debug("[NativeSymbolResolver] Module cache full, clearing ", moduleCache_.size(),
                                 " entries");
            moduleCache_.clear();
        }

        // Copy fields BEFORE the move; the moved-from info is unusable.
        if (outBaseName != nullptr)
            *outBaseName = info.baseName;
        if (outIsSystem != nullptr)
            *outIsSystem = info.isSystem;
        moduleCache_.emplace(imageBase, std::move(info));
        return true;
    }
}

std::optional<bool> NativeSymbolResolver::IsSystemModule(UINT_PTR imageBase) const
{
    bool isSystem = false;
    if (!FetchClassification(imageBase, /*outBaseName=*/nullptr, &isSystem))
    {
        return std::nullopt;
    }
    return isSystem;
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

bool NativeSymbolResolver::ResolveViaExports(UINT_PTR              ip,
                                             UINT_PTR              imageBase,
                                             const trace::WSTRING& baseName,
                                             trace::WSTRING&       outName)
{
    const DWORD rva = static_cast<DWORD>(ip - imageBase);

    char   exportNameBuf[kMaxSymbolLen + 1] = {};
    size_t exportNameLen = FindExportNameForRva(imageBase, rva, exportNameBuf, sizeof(exportNameBuf));

    if (exportNameLen == 0)
        return false;

    outName.clear();
    outName.reserve(baseName.length() + 1 + exportNameLen);

    if (!baseName.empty())
    {
        outName.append(baseName);
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

    // Extract baseName + isSystem to locals under the SRW protocol; the
    // export walk that follows runs WITHOUT the lock held, so a concurrent
    // cache eviction cannot dangle our baseName.
    trace::WSTRING baseName;
    bool           isSystem = false;
    if (!FetchClassification(imageBase, &baseName, &isSystem))
        return false;

    if (isSystem)
    {
        // System DLLs: full export symbol resolution via RVA walk.
        return ResolveViaExports(ip, imageBase, baseName, outName);
    }

    // Non-system DLLs (or export resolution disabled): module name only.
    outName.clear();
    outName.append(baseName);
    return true;
}

} // namespace ProfilerStackCapture
#endif // defined(_WIN32) && defined(_M_AMD64)

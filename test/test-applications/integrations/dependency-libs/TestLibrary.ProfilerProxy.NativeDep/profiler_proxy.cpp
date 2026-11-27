// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <cstdint>
#include <iterator>
#include <string_view>

#ifdef _WIN32
#include <windows.h>
#define PROXY_CALL __stdcall
#define PROXY_EXPORT extern "C"
#define WStr(value) L##value
using ProfilerGuid   = GUID;
using ProfilerResult = HRESULT;
#else
#include <dlfcn.h>
#define PROXY_CALL
#define PROXY_EXPORT extern "C" __attribute__((visibility("default")))

// GUID fields and HRESULT are fixed-width, including on Unix where long is 64 bits.
struct ProfilerGuid
{
    std::uint32_t data1;
    std::uint16_t data2;
    std::uint16_t data3;
    std::uint8_t  data4[8];
};

using ProfilerResult = std::int32_t;
#endif

static_assert(sizeof(ProfilerGuid) == 16);
static_assert(sizeof(ProfilerResult) == 4);

using GetClassObject = ProfilerResult(PROXY_CALL*)(const ProfilerGuid&, const ProfilerGuid&, void**);

static GetClassObject LoadProfiler()
{
    // Resolve the fixed sibling library from this proxy's location, independently of the environment.
    // Retain the module reference for the lifetime of the delegated profiler.
#ifdef _WIN32
    HMODULE proxyModule = nullptr;
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                            reinterpret_cast<LPCWSTR>(&LoadProfiler), &proxyModule))
    {
        return nullptr;
    }

    wchar_t path[32768]{};
    auto    length = GetModuleFileNameW(proxyModule, path, static_cast<DWORD>(std::size(path)));
    if (length == 0 || length >= std::size(path))
    {
        return nullptr;
    }

    const auto separator = std::wstring_view(path, length).find_last_of(WStr("\\/"));
    if (separator == std::wstring_view::npos)
    {
        return nullptr;
    }

    constexpr wchar_t profilerFileName[] = WStr("OpenTelemetry.AutoInstrumentation.Native.dll");
    const auto        directoryLength    = separator + 1;
    // Bounds are in characters, including the filename's terminator, rather than bytes.
    if (std::size(profilerFileName) > std::size(path) - directoryLength)
    {
        return nullptr;
    }

    std::copy(std::begin(profilerFileName), std::end(profilerFileName), path + directoryLength);
    auto module = LoadLibraryW(path);
    return module == nullptr ? nullptr : reinterpret_cast<GetClassObject>(GetProcAddress(module, "DllGetClassObject"));
#else
    Dl_info proxyInfo{};
    if (dladdr(reinterpret_cast<const void*>(&LoadProfiler), &proxyInfo) == 0 || proxyInfo.dli_fname == nullptr)
    {
        return nullptr;
    }

    const std::string_view proxyPath(proxyInfo.dli_fname);
    const auto             separator = proxyPath.find_last_of('/');
    if (separator == std::string_view::npos)
    {
        return nullptr;
    }

#ifdef __APPLE__
    constexpr char profilerFileName[] = "OpenTelemetry.AutoInstrumentation.Native.dylib";
#else
    constexpr char profilerFileName[] = "OpenTelemetry.AutoInstrumentation.Native.so";
#endif
    char       path[32768]{};
    const auto directoryLength = separator + 1;
    if (directoryLength > std::size(path) - std::size(profilerFileName))
    {
        return nullptr;
    }

    std::copy_n(proxyPath.data(), directoryLength, path);
    std::copy(std::begin(profilerFileName), std::end(profilerFileName), path + directoryLength);
    auto module = dlopen(path, RTLD_NOW | RTLD_LOCAL);
    return module == nullptr ? nullptr : reinterpret_cast<GetClassObject>(dlsym(module, "DllGetClassObject"));
#endif
}

PROXY_EXPORT ProfilerResult PROXY_CALL DllGetClassObject(const ProfilerGuid&, const ProfilerGuid& iid, void** result)
{
    static const auto getClassObject = LoadProfiler();
    if (getClassObject == nullptr)
    {
        return static_cast<ProfilerResult>(0x80004005u);
    }

    // Give the CLR OTel's real class factory under the proxy CLSID. The CLR then invokes
    // OTel's actual Initialize and profiler callbacks; attachment is never simulated.
    const ProfilerGuid otelProfiler = {0x918728dd, 0x259f, 0x4a6a, {0xac, 0x2b, 0xb8, 0x5e, 0x1b, 0x65, 0x83, 0x18}};
    return getClassObject(otelProfiler, iid, result);
}

PROXY_EXPORT ProfilerResult PROXY_CALL DllCanUnloadNow()
{
    return 1; // S_FALSE: keep the proxy and its delegated profiler loaded.
}

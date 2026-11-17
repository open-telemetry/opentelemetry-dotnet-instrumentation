// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include <cstdint>
#include <cstdlib>

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
#ifdef _WIN32
    wchar_t path[32768]{};
    auto    length = GetEnvironmentVariableW(WStr("OTEL_TEST_CHAINED_PROFILER_PATH"), path, 32768);
    if (length == 0 || length >= 32768)
    {
        return nullptr;
    }

    auto module = LoadLibraryW(path);
    return module == nullptr ? nullptr : reinterpret_cast<GetClassObject>(GetProcAddress(module, "DllGetClassObject"));
#else
    auto path = std::getenv("OTEL_TEST_CHAINED_PROFILER_PATH");
    if (path == nullptr)
    {
        return nullptr;
    }

    auto module = dlopen(path, RTLD_NOW | RTLD_LOCAL);
    return module == nullptr ? nullptr : reinterpret_cast<GetClassObject>(dlsym(module, "DllGetClassObject"));
#endif
    // Retain the module reference for the lifetime of the delegated profiler.
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

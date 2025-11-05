/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string_utils.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline bool IsStartupHookValid(const std::vector<WSTRING>& startup_hooks, const WSTRING& home_path)
{
    if (startup_hooks.empty())
    {
        return false;
    }

    auto startuphook_path =
        std::filesystem::path(home_path)
        / "net" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    if (!std::filesystem::exists(startuphook_path))
    {
        startuphook_path = 
            std::filesystem::path(home_path)
            / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    }
    if (!std::filesystem::exists(startuphook_path))
    {
        return false;
    }

    for (auto i = startup_hooks.begin(); i != startup_hooks.end(); i++)
    {
        const auto start_hook_path = std::filesystem::path(*i);
        std::error_code ec;
        if (std::filesystem::equivalent(startuphook_path, start_hook_path, ec))
        {
            return true;
        }
    }
    return false;
}


inline WSTRING GetStartupHookPath(const WSTRING& profiler_path)
{
    if (profiler_path == EmptyWStr)
    {
        return EmptyWStr;
    }

    // Find the StartupHook DLL based on the following possible folder structures:
    // 1. ZIP:
    //  <OTEL_HOME>/<RID>/<native_profiler_dll>
    //  <OTEL_HOME>/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    auto startuphook_path =
        std::filesystem::path(profiler_path).parent_path().parent_path()
        / "net"
        / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    if (std::filesystem::exists(startuphook_path))
    {
        return PATH_TO_WSTRING(startuphook_path);
    }

    // 2. NuGet, platform dependent deployment:
    //  <APP_FOLDER>/<native_profiler_dll>
    //  <APP_FOLDER>/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    startuphook_path =
        std::filesystem::path(profiler_path).parent_path()
        / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    if (std::filesystem::exists(startuphook_path))
    {
        return PATH_TO_WSTRING(startuphook_path);
    }

    // 3. NuGet, platform independent deployment:
    //  <APP_FOLDER>/runtimes/<RID>/<native_profiler_dll>
    //  <APP_FOLDER>/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    startuphook_path =
        std::filesystem::path(profiler_path).parent_path().parent_path().parent_path()
        / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    if (std::filesystem::exists(startuphook_path))
    {
        return PATH_TO_WSTRING(startuphook_path);
    }

    return EmptyWStr;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_
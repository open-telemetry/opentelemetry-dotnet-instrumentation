/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string_utils.h" // NOLINT
#include "managed_profiler_location_helper.h"
#include "pal.h"
#include "logger.h"

namespace trace
{

const WSTRING opentelemetry_autoinstrumentation_startuphook_filename =
    WStr("OpenTelemetry.AutoInstrumentation.StartupHook.dll");

inline bool IsStartupHookValid(const std::vector<WSTRING>& startup_hooks, const WSTRING& home_path)
{
    if (startup_hooks.empty())
    {
        return false;
    }

    const auto expected_startuphook_path =
        FindManagedAssembly(opentelemetry_autoinstrumentation_startuphook_filename, EmptyWStr, home_path);
    if (expected_startuphook_path.empty())
    {
        return false;
    }

    for (auto startup_hook = startup_hooks.begin(); startup_hook != startup_hooks.end(); startup_hook++)
    {
        const auto      startup_hook_path_candidate = std::filesystem::path(*startup_hook);
        std::error_code ec;
        const auto      equivalent =
            std::filesystem::equivalent(expected_startuphook_path.path, startup_hook_path_candidate, ec);
        if (ec)
        {
            Logger::Debug("Failed to compare StartupHook path '",
                          ToString(PATH_TO_WSTRING(expected_startuphook_path.path)), "' with '",
                          ToString(PATH_TO_WSTRING(startup_hook_path_candidate)), "': ", ec.message());
            continue;
        }

        if (equivalent)
        {
            return true;
        }
    }
    return false;
}

inline WSTRING GetStartupHookPath(const WSTRING& profiler_path, const WSTRING& home_path)
{
    if (profiler_path == EmptyWStr)
    {
        return EmptyWStr;
    }

    const auto found =
        FindManagedAssembly(opentelemetry_autoinstrumentation_startuphook_filename, profiler_path, home_path);
    return found.empty() ? EmptyWStr : PATH_TO_WSTRING(found.path);
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_STARTUP_HOOK_H_

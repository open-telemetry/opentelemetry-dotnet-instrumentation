/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string_utils.h"  // NOLINT
#include "pal.h"
#include "logger.h"

namespace trace
{

const WSTRING opentelemetry_autoinstrumentation_startuphook_filepath =
    WStr("OpenTelemetry.AutoInstrumentation.StartupHook.dll");

inline bool IsStartupHookValid(const std::vector<WSTRING>& startup_hooks, const WSTRING& home_path)
{
    if (startup_hooks.empty())
    {
        return false;
    }

    // Check for StartupHook assembly in ZIP deployment path
    auto expected_startuphook_path = std::filesystem::path(home_path) / "net"
                            / opentelemetry_autoinstrumentation_startuphook_filepath;

    std::error_code ec;
    auto path_exists = std::filesystem::exists(expected_startuphook_path, ec);
    if (ec)
    {
        Logger::Warn("Failed to access StartupHook path '",
                     ToString(PATH_TO_WSTRING(expected_startuphook_path)),
                     "': ", ec.message());
        path_exists = false;
    }

    if (!path_exists)
    {
        // Check for StartupHook assembly in NuGet deployment path
        expected_startuphook_path = std::filesystem::path(home_path)
                            / opentelemetry_autoinstrumentation_startuphook_filepath;
        ec.clear();
        path_exists = std::filesystem::exists(expected_startuphook_path, ec);
        if (ec)
        {
            Logger::Warn("Failed to access StartupHook path '",
                         ToString(PATH_TO_WSTRING(expected_startuphook_path)),
                         "': ", ec.message());
            path_exists = false;
        }
    }
    if (!path_exists)
    {
        return false;
    }

    for (auto startup_hook = startup_hooks.begin(); startup_hook != startup_hooks.end(); startup_hook++)
    {
        const auto startup_hook_path_candidate = std::filesystem::path(*startup_hook);
        ec.clear();
        const auto equivalent = std::filesystem::equivalent(expected_startuphook_path, startup_hook_path_candidate, ec);
        if (ec)
        {
            Logger::Debug("Failed to compare StartupHook path '",
                         ToString(PATH_TO_WSTRING(expected_startuphook_path)),
                         "' with '", ToString(PATH_TO_WSTRING(startup_hook_path_candidate)),
                         "': ", ec.message());
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

    const std::filesystem::path profiler(profiler_path);
    const auto profiler_directory   = profiler.parent_path();
    const auto parent_directory     = profiler_directory.parent_path();
    const auto grandparent_directory = parent_directory.parent_path();

    // Find the StartupHook DLL based on the following possible folder structures:
    std::vector<std::filesystem::path> candidate_paths;
    
    // 1. OTEL_HOME environment variable set:
    if (home_path != EmptyWStr)
    {
        const auto home_path_ = std::filesystem::path(home_path);
        // 1a. ZIP:
        //  <OTEL_HOME>/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll
        candidate_paths.push_back(home_path_ / "net" / opentelemetry_autoinstrumentation_startuphook_filepath);
        // 1b. NuGet, platform dependent deployment:
        //  <OTEL_HOME>/OpenTelemetry.AutoInstrumentation.StartupHook.dll
        candidate_paths.push_back(home_path_ / opentelemetry_autoinstrumentation_startuphook_filepath);
    }

    // 3. ZIP:
    //  <APP_FOLDER>/<native_profiler_dll>
    //  <APP_FOLDER>/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    candidate_paths.push_back(parent_directory / "net" / opentelemetry_autoinstrumentation_startuphook_filepath);
    // 4. NuGet, platform dependent deployment:
    //  <APP_FOLDER>/<native_profiler_dll>
    //  <APP_FOLDER>/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    candidate_paths.push_back(profiler_directory / opentelemetry_autoinstrumentation_startuphook_filepath);
    // 5. NuGet, platform independent deployment:
    //  <APP_FOLDER>/runtimes/<RID>/<native_profiler_dll>
    //  <APP_FOLDER>/OpenTelemetry.AutoInstrumentation.StartupHook.dll
    candidate_paths.push_back(grandparent_directory / opentelemetry_autoinstrumentation_startuphook_filepath);

    for (const auto& candidate : candidate_paths)
    {
        std::error_code ec;
        const auto exists = std::filesystem::exists(candidate, ec);
        if (ec)
        {
            Logger::Warn("Failed to evaluate for possible StartupHook '",
                         ToString(PATH_TO_WSTRING(candidate)),
                         "': ", ec.message());
            continue;
        }

        if (exists)
        {
            return PATH_TO_WSTRING(candidate);
        }
    }

    return EmptyWStr;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_

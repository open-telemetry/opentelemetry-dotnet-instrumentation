/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_
#define OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_

#include <filesystem>
#include <vector>

#include "file_utils.h"
#include "logger.h"
#include "string_utils.h"  // NOLINT

namespace trace
{

// Subdirectory names that characterize a standalone (e.g., zip/installer-based) deployment
// under OTEL_DOTNET_AUTO_HOME. Non-standalone deployments (e.g., NuGet-based) do not have these.
const WSTRING standalone_net_subdir   = WStr("net");
const WSTRING standalone_netfx_subdir = WStr("netfx");

// Checks whether path exists.
// if it does not exist or cannot be accessed (a warning is logged on error).
inline std::filesystem::path CheckAssemblyPath(const std::filesystem::path& path)
{
    std::error_code ec;
    const bool      exists = std::filesystem::exists(path, ec);
    if (ec)
    {
        Logger::Warn("Failed to check path '", ToString(PATH_TO_WSTRING(path)), "': ", ec.message());
    }
    return exists ? path : std::filesystem::path{};
}

// Returns the first existing path from candidates, or an empty path if none are found.
inline std::filesystem::path FindFirstExistingPath(const std::vector<std::filesystem::path>& candidates)
{
    for (const auto& candidate : candidates)
    {
        if (!CheckAssemblyPath(candidate).empty())
        {
            return candidate;
        }
    }
    return {};
}

// Finds the first existing path for a named managed assembly. Priority order:
//   1. tracer_home/net/<filename>              (standalone/ZIP via OTEL_DOTNET_AUTO_HOME, .NET Core)
//   2. tracer_home/netfx/<filename>            (standalone/ZIP via OTEL_DOTNET_AUTO_HOME, .NET Framework - Windows only)
//   3. tracer_home/<filename>                  (NuGet via OTEL_DOTNET_AUTO_HOME)
//   4. parent(native_dir)/net/<filename>       (standalone/ZIP, profiler-relative)
//   5. native_dir/<filename>                   (NuGet, platform-dependent)
//   6. grandparent(native_dir)/<filename>      (NuGet, platform-independent)
// Skips tracer_home candidates when tracer_home is empty, and skips profiler-relative
// candidates when profiler_path is empty.
inline std::filesystem::path FindManagedAssembly(
    const WSTRING& filename,
    const WSTRING& profiler_path,
    const WSTRING& tracer_home)
{
    std::vector<std::filesystem::path> candidates;

    if (tracer_home != EmptyWStr)
    {
        const auto home = std::filesystem::path(tracer_home);
        candidates.push_back(home / standalone_net_subdir / filename);
#ifdef _WIN32
        candidates.push_back(home / standalone_netfx_subdir / filename);
#endif
        candidates.push_back(home / filename);
    }

    if (profiler_path != EmptyWStr)
    {
        const auto profiler        = std::filesystem::path(profiler_path);
        const auto profiler_dir    = profiler.parent_path();
        const auto parent_dir      = profiler_dir.parent_path();
        const auto grandparent_dir = parent_dir.parent_path();
        candidates.push_back(parent_dir / standalone_net_subdir / filename);
        candidates.push_back(profiler_dir / filename);
        candidates.push_back(grandparent_dir / filename);
    }

    return FindFirstExistingPath(candidates);
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_

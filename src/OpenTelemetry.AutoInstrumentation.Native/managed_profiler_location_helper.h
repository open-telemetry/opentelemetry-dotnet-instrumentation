/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_
#define OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_

#include <filesystem>
#include <utility>
#include <vector>

#include "file_utils.h"
#include "logger.h"
#include "string_utils.h" // NOLINT

namespace trace
{

namespace detail
{
const WSTRING net_subdir   = WStr("net");
const WSTRING netfx_subdir = WStr("netfx");
} // namespace detail

// Represents the result of a managed assembly search.
struct AssemblyLocation
{
    std::filesystem::path path;
    bool                  is_standalone = false; // true if found in a standalone (net/netfx) layout

    bool empty() const
    {
        return path.empty();
    }
};

// Finds the first existing path for a named managed assembly, and reports whether
// the match is from a standalone (net/netfx subdirectory) or NuGet-based layout.
// Priority order:
//   1. tracer_home/net/<filename>              (standalone/ZIP via OTEL_DOTNET_AUTO_HOME, .NET Core)
//   2. tracer_home/netfx/<filename>            (standalone/ZIP via OTEL_DOTNET_AUTO_HOME, .NET Framework - Windows
//   only)
//   3. tracer_home/<filename>                  (NuGet via OTEL_DOTNET_AUTO_HOME)
//   4. parent(native_dir)/net/<filename>       (standalone/ZIP, profiler-relative)
//   5. native_dir/<filename>                   (NuGet, platform-dependent)
//   6. grandparent(native_dir)/<filename>      (NuGet, platform-independent)
// Skips tracer_home candidates when tracer_home is empty, and skips profiler-relative
// candidates when profiler_path is empty.
inline AssemblyLocation FindManagedAssembly(const WSTRING& filename,
                                            const WSTRING& profiler_path,
                                            const WSTRING& tracer_home)
{
    std::vector<std::pair<std::filesystem::path, bool>> candidates;

    if (tracer_home != EmptyWStr)
    {
        const auto home = std::filesystem::path(tracer_home);
        candidates.emplace_back(home / detail::net_subdir / filename, true);
#ifdef _WIN32
        candidates.emplace_back(home / detail::netfx_subdir / filename, true);
#endif
        candidates.emplace_back(home / filename, false);
    }

    if (profiler_path != EmptyWStr)
    {
        const auto profiler_dir    = std::filesystem::path(profiler_path).parent_path();
        const auto parent_dir      = profiler_dir.parent_path();
        const auto grandparent_dir = parent_dir.parent_path();
        candidates.emplace_back(parent_dir / detail::net_subdir / filename, true);
        candidates.emplace_back(profiler_dir / filename, false);
        candidates.emplace_back(grandparent_dir / filename, false);
    }

    for (const auto& [path, is_standalone] : candidates)
    {
        std::error_code ec;
        const bool      exists = std::filesystem::exists(path, ec);
        if (ec)
        {
            Logger::Warn("Failed to check path '", ToString(PATH_TO_WSTRING(path)), "': ", ec.message());
        }
        if (exists)
        {
            return {path, is_standalone};
        }
    }
    return {};
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_MANAGED_PROFILER_LOCATION_HELPER_H_

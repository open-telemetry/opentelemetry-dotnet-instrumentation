#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline const std::filesystem::path GetExpectedStartupHookPath(const WSTRING& home_path) {
    std::filesystem::path path = home_path;
    return (path / "netcoreapp3.1" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll");
}

inline bool IsStartupHookEnabled(const WSTRING& startup_hooks, const WSTRING& home_path)
{
    const std::filesystem::path expected_startuphook_path = GetExpectedStartupHookPath(home_path);
    const std::filesystem::path actual_startup_path = startup_hooks;

    return actual_startup_path == expected_startuphook_path;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_
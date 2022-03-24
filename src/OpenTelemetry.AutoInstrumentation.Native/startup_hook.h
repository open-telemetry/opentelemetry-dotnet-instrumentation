#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline bool IsStartupHookValid(const std::vector<WSTRING>& startup_hooks, const WSTRING& home_path)
{
    if (startup_hooks.empty())
    {
        return false;
    }

    const auto expected_startuphook_path =
        std::filesystem::path(home_path)
        / "netcoreapp3.1" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";
    if (!std::filesystem::exists(expected_startuphook_path))
    {
        return false;
    }

    for (auto i = startup_hooks.begin(); i != startup_hooks.end(); i++)
    {
        const auto start_hook_path = std::filesystem::path(*i);
        std::error_code ec;
        if (std::filesystem::equivalent(expected_startuphook_path, start_hook_path, ec))
        {
            return true;
        }
    }
    return false;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_
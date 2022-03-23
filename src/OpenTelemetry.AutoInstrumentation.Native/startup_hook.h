#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline bool IsStartupHookEnabled(const std::vector<WSTRING>& startup_hooks, const WSTRING& home_path)
{
    if (startup_hooks.empty())
    {
        return false;
    }

    const auto expected_startuphook_path = std::filesystem::path(home_path)
        / "netcoreapp3.1" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll";

    for (auto i = startup_hooks.begin(); i != startup_hooks.end(); i++)
    {
        if (std::filesystem::path(*i) == expected_startuphook_path)
        {
            return true;
        }
    }
    return false;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_
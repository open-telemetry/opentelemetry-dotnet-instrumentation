#ifndef OTEL_CLR_PROFILER_STARTUP_HOOK_H_
#define OTEL_CLR_PROFILER_STARTUP_HOOK_H_

#include "string.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline WSTRING GetExpectedStartupHookPath(const WSTRING& home_path) {
    WSTRING separator = EmptyWStr;
    if (home_path.back() != DIR_SEPARATOR) {
        separator = DIR_SEPARATOR;
    }

    return home_path + separator + WStr("netcoreapp3.1") + DIR_SEPARATOR +
            WStr("OpenTelemetry.Instrumentation.StartupHook.dll");
}

inline bool IsStartupHookEnabled(const WSTRING& startup_hooks, const WSTRING& home_path)
{
    const WSTRING expected_startup_hook = GetExpectedStartupHookPath(home_path);
    auto n = startup_hooks.find(expected_startup_hook);

    return n != WSTRING::npos;
}

}  // namespace trace

#endif  // OTEL_CLR_PROFILER_STARTUP_HOOK_H_
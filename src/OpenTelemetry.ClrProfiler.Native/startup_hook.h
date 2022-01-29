#ifndef DD_CLR_PROFILER_STARTUP_HOOK_H_
#define DD_CLR_PROFILER_STARTUP_HOOK_H_

#include "string.h"  // NOLINT
#include "pal.h"

namespace trace
{

inline WSTRING GetExpectedStartupHookPath(WSTRING home_path) {
    if (home_path.back() != DIR_SEPARATOR) {
        home_path.push_back(DIR_SEPARATOR);
    }

    return home_path + WStr("netcoreapp3.1") + DIR_SEPARATOR +
            WStr("OpenTelemetry.Instrumentation.StartupHook.dll");
}

inline bool IsStartupHookEnabled(WSTRING startup_hooks, WSTRING home_path)
{
    WSTRING expected_startup_hook = GetExpectedStartupHookPath(home_path);
    auto n = startup_hooks.find(expected_startup_hook);

    return n != WSTRING::npos;
}

}  // namespace trace

#endif  // DD_CLR_PROFILER_STARTUP_HOOK_H_
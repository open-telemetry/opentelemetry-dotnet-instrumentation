/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_PAL_H_
#define OTEL_CLR_PROFILER_PAL_H_

#ifdef _WIN32

#include "windows.h"
#include <process.h>

#else

#include <fstream>
#include <dlfcn.h>

#endif

#include <filesystem>

#if MACOS
#include <libproc.h>
#endif

#include "environment_variables.h"
#include "string_utils.h" // NOLINT
#include "file_utils.h"
#include "util.h"

namespace trace
{

template <class TLoggerPolicy>
inline WSTRING GetOpenTelemetryLogFilePath(const std::string& file_name_suffix)
{
    const auto file_name = TLoggerPolicy::file_name + file_name_suffix + ".log";

    std::filesystem::path directory = GetEnvironmentValue(environment::log_directory);

    if (!directory.empty())
    {
        return PATH_TO_WSTRING(directory / file_name);
    }
#ifdef _WIN32
    std::filesystem::path program_data_path;
    program_data_path = GetEnvironmentValue(WStr("PROGRAMDATA"));

    if (program_data_path.empty())
    {
        program_data_path = WStr(R"(C:\ProgramData)");
    }

    return PATH_TO_WSTRING(program_data_path / TLoggerPolicy::folder_path  / file_name);
#else
    std::filesystem::path program_data_path = WStr("/var/log/opentelemetry/dotnet/");
    return PATH_TO_WSTRING(program_data_path / file_name);
#endif
}

inline WSTRING GetCurrentProcessName()
{
#ifdef _WIN32
    const DWORD length = 260;
    WCHAR buffer[length]{};

    const DWORD len = GetModuleFileName(nullptr, buffer, length);
    const WSTRING current_process_path(buffer);
    return std::filesystem::path(current_process_path).filename();
#elif MACOS
    const int length = 260;
    char* buffer = new char[length];
    proc_name(getpid(), buffer, length);
    return ToWSTRING(std::string(buffer));
#else
    std::fstream comm("/proc/self/comm");
    std::string name;
    std::getline(comm, name);
    return ToWSTRING(name);
#endif
}

inline int GetPID()
{
#ifdef _WIN32
    return _getpid();
#else
    return getpid();
#endif
}

inline WSTRING GetCurrentModuleFileName()
{
    static WSTRING moduleFileName = EmptyWStr;
    if (moduleFileName != EmptyWStr)
    {
        // use cached version
        return moduleFileName;
    }

#ifdef _WIN32
    HMODULE hModule;
    if (GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT | GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
                           (LPCTSTR) GetCurrentModuleFileName,
                           &hModule))
    {
        WCHAR lpFileName[1024];
        DWORD lpFileNameLength = GetModuleFileNameW(hModule, lpFileName, 1024);
        if (lpFileNameLength > 0)
        {
            moduleFileName = WSTRING(lpFileName, lpFileNameLength);
            return moduleFileName;
        }
    }
#else
    Dl_info info;
    if (dladdr((void*)GetCurrentModuleFileName, &info))
    {
        moduleFileName = ToWSTRING(ToString(info.dli_fname));
        return moduleFileName;
    }
#endif

    return EmptyWStr;
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_PAL_H_

#ifndef OTEL_CLR_PROFILER_PAL_H_
#define OTEL_CLR_PROFILER_PAL_H_

#ifdef _WIN32

#include "windows.h"
#include <filesystem>
#include <process.h>

#else

#include <fstream>
#include <unistd.h>

#endif

#if MACOS
#include <libproc.h>
#endif

#include "environment_variables.h"
#include "string.h" // NOLINT
#include "util.h"

#ifdef _WIN32
#define DIR_SEPARATOR WStr('\\')
#define ENV_VAR_PATH_SEPARATOR WStr(';')
#else
#define DIR_SEPARATOR WStr('/')
#define ENV_VAR_PATH_SEPARATOR WStr(':')
#endif

namespace trace
{

template <class TLoggerPolicy>
inline WSTRING GetDatadogLogFilePath(const std::string& file_name_suffix)
{
    const auto file_name = TLoggerPolicy::file_name + file_name_suffix + ".log";

    WSTRING directory = GetEnvironmentValue(environment::log_directory);

    if (directory.length() > 0)
    {
        return directory + DIR_SEPARATOR + ToWSTRING(file_name);
    }

    WSTRING path = GetEnvironmentValue(TLoggerPolicy::logging_environment::log_path);

    if (path.length() > 0)
    {
        return path;
    }

#ifdef _WIN32
    std::filesystem::path program_data_path;
    program_data_path = GetEnvironmentValue(WStr("PROGRAMDATA"));

    if (program_data_path.empty())
    {
        program_data_path = WStr(R"(C:\ProgramData)");
    }

    // on Windows WSTRING == wstring
    return (program_data_path / TLoggerPolicy::folder_path  / file_name).wstring();
#else
    return ToWSTRING("/var/log/opentelemetry/dotnet/" + file_name);
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

} // namespace trace

#endif // OTEL_CLR_PROFILER_PAL_H_

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_LOGGER_IMPL_H_
#define OTEL_CLR_PROFILER_LOGGER_IMPL_H_
#include "util.h"
#include "environment_variables.h"
#include "string.h"
#include "pal.h"

#include "spdlog/sinks/null_sink.h"
#include "spdlog/sinks/rotating_file_sink.h"
#include "spdlog/sinks/stdout_sinks.h"

#ifndef _WIN32
typedef struct stat Stat;
#endif

#include <spdlog/spdlog.h>

#include <iostream>
#include <memory>
#include <filesystem>

namespace trace
{

template <typename TLoggerPolicy>
class LoggerImpl : public Singleton<LoggerImpl<TLoggerPolicy>>
{
    friend class Singleton<LoggerImpl<TLoggerPolicy>>;

private:
    std::shared_ptr<spdlog::logger> m_fileout;
    static std::string GetLogPath(const std::string& file_name_suffix);
    static std::shared_ptr<spdlog::logger> CreateFileSink(std::string logger_name);
    LoggerImpl();
    ~LoggerImpl();

    static inline const WSTRING log_level_none  = WStr("none");
    static inline const WSTRING log_level_error = WStr("error");
    static inline const WSTRING log_level_warn  = WStr("warn");
    static inline const WSTRING log_level_info  = WStr("info");
    static inline const WSTRING log_level_debug = WStr("debug");

    static inline const WSTRING log_sink_none    = WStr("none");
    static inline const WSTRING log_sink_file    = WStr("file");
    static inline const WSTRING log_sink_console = WStr("console");

    static inline const std::string logger_name = "Logger";

    bool ShouldLog(spdlog::level::level_enum log_level);

public:
    template <typename... Args>
    void Debug(const Args&... args);

    template <typename... Args>
    void Info(const Args&... args);

    template <typename... Args>
    void Warn(const Args&... args);

    template <typename... Args>
    void Error(const Args&... args);

    template <typename... Args>
    void Critical(const Args&... args);

    void Flush();

    bool IsDebugEnabled() const;

    static void Shutdown()
    {
        spdlog::shutdown();
    }
};

template <typename TLoggerPolicy>
std::string LoggerImpl<TLoggerPolicy>::GetLogPath(const std::string& file_name_suffix)
{
    const auto path = ToString(GetOpenTelemetryLogFilePath<TLoggerPolicy>(file_name_suffix));

    const auto log_path = std::filesystem::path(path);

    if (log_path.has_parent_path())
    {
        const auto parent_path = log_path.parent_path();

        if (!std::filesystem::exists(parent_path))
        {
            std::filesystem::create_directories(parent_path);
        }
    }

    return path;
}

template <typename TLoggerPolicy>
std::shared_ptr<spdlog::logger> LoggerImpl<TLoggerPolicy>::CreateFileSink(std::string logger_name)
{
    static auto current_process_name = ToString(GetCurrentProcessName());
    static auto current_process_id   = GetPID();
    static auto current_process_without_extension =
        current_process_name.substr(0, current_process_name.find_last_of("."));

    static auto file_name_suffix =
        std::to_string(current_process_id) + "-" + current_process_without_extension + "-Native";

    // by default, use the same size as on managed side: 10MiB
    static auto file_size = GetConfiguredSize(environment::max_log_file_size, 10485760);

    static std::shared_ptr<spdlog::logger> fileout;

    try
    {
        fileout = spdlog::rotating_logger_mt(logger_name, GetLogPath(file_name_suffix), file_size, 10);
    }
    catch (...)
    {
        // By writing into the stderr was changing the behavior in a CI scenario.
        // There's not a good way to report errors when trying to create the log file.
        // But we never should be changing the normal behavior of an app.
        // std::cerr << "LoggerImpl Handler: Error creating native log file." << std::endl;
        fileout = spdlog::null_logger_mt(logger_name);
    }

    return fileout;
}

template <typename TLoggerPolicy>
LoggerImpl<TLoggerPolicy>::LoggerImpl()
{
    spdlog::set_error_handler([](const std::string& msg) {
        // By writing into the stderr was changing the behavior in a CI scenario.
        // There's not a good way to report errors when trying to create the log file.
        // But we never should be changing the normal behavior of an app.
        // std::cerr << "LoggerImpl Handler: " << msg << std::endl;
    });

    static auto configured_log_level = GetEnvironmentValue(environment::log_level);

    if (configured_log_level == log_level_none)
    {
        m_fileout = spdlog::null_logger_mt(logger_name);
        return;
    }
    auto log_level = spdlog::level::info;

    if (configured_log_level == log_level_error)
    {
        log_level = spdlog::level::err;
    }
    else if (configured_log_level == log_level_warn)
    {
        log_level = spdlog::level::warn;
    }
    else if (configured_log_level == log_level_info)
    {
        log_level = spdlog::level::info;
    }
    else if (configured_log_level == log_level_debug)
    {
        log_level = spdlog::level::debug;
    }

    spdlog::flush_every(std::chrono::seconds(3));

    static auto configured_log_sink = GetEnvironmentValue(environment::log_sink);

    if (configured_log_sink == log_sink_none)
    {
        m_fileout = spdlog::null_logger_mt(logger_name);
        return;
    }
    else if (configured_log_sink == log_sink_console)
    {
        m_fileout = spdlog::stdout_logger_mt(logger_name);
    }
    // Default to file sink
    else
    {
        // Creates file sink, if file sink creation fails fallbacks to NoOp sink.
        m_fileout = CreateFileSink(logger_name);
    }

    m_fileout->set_level(log_level);

    m_fileout->set_pattern(TLoggerPolicy::pattern, spdlog::pattern_time_type::utc);

    // trigger flush whenever info messages are logged
    m_fileout->flush_on(spdlog::level::info);
};

template <typename TLoggerPolicy>
LoggerImpl<TLoggerPolicy>::~LoggerImpl()
{
    m_fileout->flush();
    spdlog::shutdown();
};

template <class T>
void WriteToStream(std::ostringstream& oss, T const& x)
{
    if constexpr (std::is_same<T, WSTRING>::value)
    {
        oss << ToString(x);
    }
    else
    {
        oss << x;
    }
}

template <typename... Args>
static std::string LogToString(Args const&... args)
{
    std::ostringstream oss;
    (WriteToStream(oss, args), ...);

    return oss.str();
}

template <typename TLoggerPolicy>
template <typename... Args>
void LoggerImpl<TLoggerPolicy>::Debug(const Args&... args)
{
    // to avoid possibly unnecessary LogToString conversion, check log level before calling underlying logger
    if (ShouldLog(spdlog::level::debug))
    {
        m_fileout->debug(LogToString(args...));
    }
}

template <typename TLoggerPolicy>
template <typename... Args>
void LoggerImpl<TLoggerPolicy>::Info(const Args&... args)
{
    // to avoid possibly unnecessary LogToString conversion, check log level before calling underlying logger
    if (ShouldLog(spdlog::level::info))
    {
        m_fileout->info(LogToString(args...));
    }
}

template <typename TLoggerPolicy>
template <typename... Args>
void LoggerImpl<TLoggerPolicy>::Warn(const Args&... args)
{
    // to avoid possibly unnecessary LogToString conversion, check log level before calling underlying logger
    if (ShouldLog(spdlog::level::warn))
    {
        m_fileout->warn(LogToString(args...));
    }
}

template <typename TLoggerPolicy>
template <typename... Args>
void LoggerImpl<TLoggerPolicy>::Error(const Args&... args)
{
    // to avoid possibly unnecessary LogToString conversion, check log level before calling underlying logger
    if (ShouldLog(spdlog::level::err))
    {
        m_fileout->error(LogToString(args...));
    }
}

template <typename TLoggerPolicy>
template <typename... Args>
void LoggerImpl<TLoggerPolicy>::Critical(const Args&... args)
{
    // to avoid possibly unnecessary LogToString conversion, check log level before calling underlying logger
    if (ShouldLog(spdlog::level::critical))
    {
        m_fileout->critical(LogToString(args...));
    }
}

template <typename TLoggerPolicy>
bool LoggerImpl<TLoggerPolicy>::ShouldLog(spdlog::level::level_enum log_level)
{
    return m_fileout->should_log(log_level);
}

template <typename TLoggerPolicy>
void LoggerImpl<TLoggerPolicy>::Flush()
{
    m_fileout->flush();
}

template <typename TLoggerPolicy>
bool LoggerImpl<TLoggerPolicy>::IsDebugEnabled() const
{
    return m_fileout->level() == spdlog::level::debug;
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_LOGGER_IMPL_H_

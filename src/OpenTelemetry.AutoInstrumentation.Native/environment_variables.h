/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_
#define OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_

#include "string_utils.h"  // NOLINT

namespace trace {
namespace environment {

// Sets logging level used by autoinstrumentation loggers
const WSTRING log_level = WStr("OTEL_LOG_LEVEL");

// Sets logger used by autoinstrumentation
const WSTRING log_sink = WStr("OTEL_DOTNET_AUTO_LOGGER");

// Sets max size of a single log file
const WSTRING max_log_file_size = WStr("OTEL_DOTNET_AUTO_LOG_FILE_SIZE");

// Sets the path to the profiler's home directory, for example:
// "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\" or "/opt/opentelemetry/"
const WSTRING profiler_home_path = WStr("OTEL_DOTNET_AUTO_HOME");

// Sets the filename of executables the profiler cannot attach to.
// If not defined (default), the profiler will attach to any process.
// Supports multiple values separated with comma, for example:
// "MyApp.exe,dotnet.exe"
const WSTRING exclude_process_names = WStr("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES");


// Sets the directory for the profiler's log file.
// If not set, default is
// "%ProgramData%"\OpenTelemetry .NET AutoInstrumentation\logs\" on Windows or
// "/var/log/opentelemetry/dotnet/" on Linux.
const WSTRING log_directory = WStr("OTEL_DOTNET_AUTO_LOG_DIRECTORY");

// Sets whether to disable all JIT optimizations.
// Default value is false (do not disable all optimizations).
// https://github.com/dotnet/coreclr/issues/24676
// https://github.com/dotnet/coreclr/issues/12468
const WSTRING clr_disable_optimizations = WStr("OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS");

// Indicates whether the profiler is running in the context
// of Azure App Services
const WSTRING azure_app_services = WStr("OTEL_DOTNET_AUTO_AZURE_APP_SERVICES");

// The app_pool_id in the context of azure app services
const WSTRING azure_app_services_app_pool_id = WStr("APP_POOL_ID");

// The DOTNET_CLI_TELEMETRY_PROFILE in the context of azure app services
const WSTRING azure_app_services_cli_telemetry_profile_value =
    WStr("DOTNET_CLI_TELEMETRY_PROFILE");

// Enable the profiler to dump the IL original code and modification to the log.
const WSTRING dump_il_rewrite_enabled = WStr("OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED");

// Sets whether to enable JIT inlining
const WSTRING clr_enable_inlining = WStr("OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING");

// Sets whether to enable NGEN images.
const WSTRING clr_enable_ngen = WStr("OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN");

// Enable the assembly version redirection when running on the .NET Framework.
const WSTRING netfx_assembly_redirection_enabled = WStr("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED");

// Enable the fail fast mode.
const WSTRING fail_fast_enabled = WStr("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED");

// Enable the IL rewrite of SqlClient instrumentation for .NET Framework applications to capture command text.
const WSTRING sqlclient_netfx_ilrewrite_enabled = WStr("OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED");

// The list of startup hooks defined for .NET Core 3.1+ applications.
// This is a .NET runtime environment variable. 
// See https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md
// for more information about this environment variable.
const WSTRING dotnet_startup_hooks = WStr("DOTNET_STARTUP_HOOKS");

const WSTRING prefix_cor = WStr("COR_");
const WSTRING prefix_coreclr = WStr("CORECLR_");
const WSTRING prefix_dotnet = WStr("DOTNET_");
const WSTRING prefix_otel = WStr("OTEL_");

}  // namespace environment
}  // namespace trace

#endif

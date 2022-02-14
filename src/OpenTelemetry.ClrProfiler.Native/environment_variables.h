#ifndef OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_
#define OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_

#include "string.h"  // NOLINT

namespace trace {
namespace environment {

// Sets whether the profiler is enabled. Default is true.
// Setting this to false disabled the profiler entirely.
const WSTRING tracing_enabled = WStr("OTEL_DOTNET_AUTO_ENABLED");

// Sets whether debug mode is enabled. Default is false.
const WSTRING debug_enabled = WStr("OTEL_TRACE_DEBUG");

// Sets the paths to integration definition JSON files.
// Supports multiple values separated with comma, for example:
// "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\integrations.json,D:\temp\test_integrations.json"
const WSTRING integrations_path = WStr("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE");

// Sets the path to the profiler's home directory, for example:
// "C:\Program Files\OpenTelemetry .NET AutoInstrumentation\" or "/opt/opentelemetry/"
const WSTRING profiler_home_path = WStr("OTEL_DOTNET_AUTO_HOME");

// Sets the filename of executables the profiler can attach to.
// If not defined (default), the profiler will attach to any process.
// Supports multiple values separated with comma, for example:
// "MyApp.exe,dotnet.exe"
const WSTRING include_process_names = WStr("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES");

// Sets the filename of executables the profiler cannot attach to.
// If not defined (default), the profiler will attach to any process.
// Supports multiple values separated with comma, for example:
// "MyApp.exe,dotnet.exe"
const WSTRING exclude_process_names = WStr("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES");

// Sets a list of integrations to disable. All other integrations will remain
// enabled. If not set (default), all integrations are enabled. Supports
// multiple values separated with comma, for example:
// "ElasticsearchNet,AspNetWebApi2"
const WSTRING disabled_integrations =
    WStr("OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS");

// Sets the path for the profiler's log file.
// Environment variable OTEL_TRACE_LOG_DIRECTORY takes precedence over this setting, if set.
const WSTRING log_path = WStr("OTEL_TRACE_LOG_PATH");

// Sets the directory for the profiler's log file.
// If set, this setting takes precedence over environment variable OTEL_TRACE_LOG_PATH.
// If not set, default is
// "%ProgramData%"\OpenTelemetry .NET AutoInstrumentation\logs\" on Windows or
// "/var/log/opentelemetry/dotnet/" on Linux.
const WSTRING log_directory = WStr("OTEL_TRACE_LOG_DIRECTORY");

// Sets whether to disable all JIT optimizations.
// Default value is false (do not disable all optimizations).
// https://github.com/dotnet/coreclr/issues/24676
// https://github.com/dotnet/coreclr/issues/12468
const WSTRING clr_disable_optimizations = WStr("OTEL_DOTNET_AUTO_CLR_DISABLE_OPTIMIZATIONS");

// Sets whether to intercept method calls when the caller method is inside a
// domain-neutral assembly. This is dangerous because the integration assembly
// the user can guarantee that all Application Pools on the system have at most
// Default is false. Only used in .NET Framework 4.5 and 4.5.1.
// https://github.com/DataDog/dd-trace-dotnet/pull/671
const WSTRING domain_neutral_instrumentation = WStr("OTEL_DOTNET_AUTO_DOMAIN_NEUTRAL_INSTRUMENTATION");

// Indicates whether the profiler is running in the context
// of Azure App Services
const WSTRING azure_app_services = WStr("OTEL_DOTNET_AUTO_AZURE_APP_SERVICES");

// The app_pool_id in the context of azure app services
const WSTRING azure_app_services_app_pool_id = WStr("APP_POOL_ID");

// The DOTNET_CLI_TELEMETRY_PROFILE in the context of azure app services
const WSTRING azure_app_services_cli_telemetry_profile_value =
    WStr("DOTNET_CLI_TELEMETRY_PROFILE");

// Enable the profiler to dump the IL original code and modification to the log.
const WSTRING dump_il_rewrite_enabled = WStr("OTEL_DUMP_ILREWRITE_ENABLED");

// Sets whether to enable JIT inlining
const WSTRING clr_enable_inlining = WStr("OTEL_DOTNET_AUTO_CLR_ENABLE_INLINING");

// Custom internal tracer profiler path
const WSTRING internal_trace_profiler_path =
    WStr("OTEL_INTERNAL_TRACE_NATIVE_ENGINE_PATH");

// Sets whether to enable NGEN images.
const WSTRING clr_enable_ngen = WStr("OTEL_DOTNET_AUTO_CLR_ENABLE_NGEN");

// The list of startup hooks defined for .NET Core 3.1+ applications.
// This is a .NET runtime environment variable. 
// See https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md
// for more information about this environment variable.
const WSTRING dotnet_startup_hooks = WStr("DOTNET_STARTUP_HOOKS");

}  // namespace environment
}  // namespace trace

#endif

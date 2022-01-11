#ifndef DD_PROFILER_CONSTANTS_H
#define DD_PROFILER_CONSTANTS_H

#include <string>

#include "environment_variables.h"
#include "logger.h"

namespace trace
{

const WSTRING env_vars_to_display[]{environment::tracing_enabled,
                                    environment::debug_enabled,
                                    environment::calltarget_enabled,
                                    environment::profiler_home_path,
                                    environment::integrations_path,
                                    environment::include_process_names,
                                    environment::exclude_process_names,
                                    environment::disabled_integrations,
                                    environment::log_path,
                                    environment::log_directory,
                                    environment::clr_disable_optimizations,
                                    environment::clr_enable_inlining,
                                    environment::clr_enable_ngen,
                                    environment::domain_neutral_instrumentation,
                                    environment::dump_il_rewrite_enabled,
                                    environment::netstandard_enabled,
                                    environment::azure_app_services,
                                    environment::azure_app_services_app_pool_id,
                                    environment::azure_app_services_cli_telemetry_profile_value,
                                    environment::internal_trace_profiler_path};

const WSTRING skip_assembly_prefixes[]{
    WStr("Microsoft.AI"),
    WStr("Microsoft.ApplicationInsights"),
    WStr("Microsoft.Build"),
    WStr("Microsoft.CSharp"),
    WStr("Microsoft.Extensions.Caching"),
    WStr("Microsoft.Extensions.Configuration"),
    WStr("Microsoft.Extensions.DependencyInjection"),
    WStr("Microsoft.Extensions.DependencyModel"),
    WStr("Microsoft.Extensions.Diagnostics"),
    WStr("Microsoft.Extensions.FileProviders"),
    WStr("Microsoft.Extensions.FileSystemGlobbing"),
    WStr("Microsoft.Extensions.Hosting"),
    WStr("Microsoft.Extensions.Http"),
    WStr("Microsoft.Extensions.Identity"),
    WStr("Microsoft.Extensions.Localization"),
    WStr("Microsoft.Extensions.ObjectPool"),
    WStr("Microsoft.Extensions.Options"),
    WStr("Microsoft.Extensions.PlatformAbstractions"),
    WStr("Microsoft.Extensions.Primitives"),
    WStr("Microsoft.Extensions.WebEncoders "),
    WStr("Microsoft.Web.Compilation.Snapshots"),
    WStr("System.Core"),
    WStr("System.Console"),
    WStr("System.Collections"),
    WStr("System.ComponentModel"),
    WStr("System.Diagnostics"),
    WStr("System.Drawing"),
    WStr("System.EnterpriseServices"),
    WStr("System.IO"),
    WStr("System.Runtime"),
    WStr("System.Text"),
    WStr("System.Threading"),
    WStr("System.Xml"),
};

const WSTRING skip_assemblies[]{WStr("mscorlib"),
                                WStr("netstandard"),
                                WStr("System.Configuration"),
                                WStr("Microsoft.AspNetCore.Razor.Language"),
                                WStr("Microsoft.AspNetCore.Mvc.RazorPages"),
                                WStr("Anonymously Hosted DynamicMethods Assembly"),
                                WStr("ISymWrapper")};

const WSTRING mscorlib_assemblyName = WStr("mscorlib");
const WSTRING system_private_corelib_assemblyName = WStr("System.Private.CoreLib");
const WSTRING opentelemetry_clrprofiler_managed_loader_assemblyName = WStr("OpenTelemetry.ClrProfiler.Managed.Loader");

const WSTRING microsoft_aspnetcore_hosting_assemblyName = WStr("Microsoft.AspNetCore.Hosting");
const WSTRING dapper_assemblyName = WStr("Dapper");

const WSTRING managed_profiler_full_assembly_version =
    WStr("OpenTelemetry.ClrProfiler.Managed, Version=0.0.1.0, Culture=neutral, PublicKeyToken=34b8972644a12429");

const WSTRING managed_profiler_name = WStr("OpenTelemetry.ClrProfiler.Managed");

const WSTRING nonwindows_nativemethods_type = WStr("OpenTelemetry.ClrProfiler.Managed.NativeMethods+NonWindows");

const WSTRING calltarget_modification_action = WStr("CallTargetModification");

} // namespace trace

#endif // DD_PROFILER_CONSTANTS_H

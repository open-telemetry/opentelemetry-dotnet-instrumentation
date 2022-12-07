#ifndef OTEL_PROFILER_CONSTANTS_H
#define OTEL_PROFILER_CONSTANTS_H

#include <string>

#include "environment_variables.h"

namespace trace
{
const std::vector env_vars_prefixes_to_display{environment::prefix_cor,
                                               environment::prefix_coreclr,
                                               environment::prefix_dotnet,
                                               environment::prefix_otel,
                                               environment::azure_app_services_app_pool_id};

const WSTRING mscorlib_assemblyName = WStr("mscorlib");
const WSTRING system_private_corelib_assemblyName = WStr("System.Private.CoreLib");
const WSTRING opentelemetry_autoinstrumentation_loader_assemblyName = WStr("OpenTelemetry.AutoInstrumentation.Loader");

const WSTRING managed_profiler_name = WStr("OpenTelemetry.AutoInstrumentation");

const WSTRING managed_profiler_full_assembly_version =
    WStr("OpenTelemetry.AutoInstrumentation, Version=0.5.1.0, Culture=neutral, PublicKeyToken=null");

const WSTRING managed_profiler_full_assembly_version_strong_name =
    WStr("OpenTelemetry.AutoInstrumentation, Version=0.5.1.0, Culture=neutral, PublicKeyToken=c0db600a13f60b51");

const WSTRING nonwindows_nativemethods_type = WStr("OpenTelemetry.AutoInstrumentation.NativeMethods+NonWindows");

const WSTRING calltarget_modification_action = WStr("CallTargetModification");

} // namespace trace

#endif // OTEL_PROFILER_CONSTANTS_H

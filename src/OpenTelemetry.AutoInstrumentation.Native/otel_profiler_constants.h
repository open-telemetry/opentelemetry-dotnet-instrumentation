/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_PROFILER_CONSTANTS_H
#define OTEL_PROFILER_CONSTANTS_H

#include <string>

#include "environment_variables.h"
#include "version.h"

namespace trace
{
const std::vector env_vars_prefixes_to_display{environment::prefix_cor,
                                               environment::prefix_coreclr,
                                               environment::prefix_dotnet,
                                               environment::prefix_otel,
                                               environment::azure_app_services_app_pool_id};

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
    WStr("Microsoft.Extensions.WebEncoders"),
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
const WSTRING opentelemetry_autoinstrumentation_loader_assemblyName = WStr("OpenTelemetry.AutoInstrumentation.Loader");

const WSTRING managed_profiler_name = WStr("OpenTelemetry.AutoInstrumentation");

#ifdef _WIN32
const WSTRING windows_nativemethods_type = WStr("OpenTelemetry.AutoInstrumentation.NativeMethods+Windows");
#else
const WSTRING nonwindows_nativemethods_type = WStr("OpenTelemetry.AutoInstrumentation.NativeMethods+NonWindows");
#endif // _WIN32

const WSTRING managed_profiler_full_assembly_version =
    WStr("OpenTelemetry.AutoInstrumentation, Version=") + ToWSTRING(ASSEMBLY_VERSION) + WStr(", Culture=neutral, PublicKeyToken=null");

const WSTRING managed_profiler_full_assembly_version_strong_name =
    WStr("OpenTelemetry.AutoInstrumentation, Version=") + ToWSTRING(ASSEMBLY_VERSION) + WStr(", Culture=neutral, PublicKeyToken=c0db600a13f60b51");

} // namespace trace

#endif // OTEL_PROFILER_CONSTANTS_H

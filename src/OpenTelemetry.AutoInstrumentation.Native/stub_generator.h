/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STUB_GENERATOR_H_
#define OTEL_CLR_PROFILER_STUB_GENERATOR_H_

#include <corhlpr.h>
#include <corprof.h>
#include <string>

#include "string_utils.h"
#include "clr_helpers.h"

namespace trace
{
class CorProfiler;

class StubGenerator
{
private:
    CorProfiler*            m_profiler;
    ICorProfilerInfo7*      m_pICorProfilerInfo;
    const AssemblyProperty& m_corAssemblyProperty;

public:
    StubGenerator(CorProfiler* profiler,
                  ICorProfilerInfo7* pICorProfilerInfo,
                  const AssemblyProperty& corAssemblyProperty);

    ~StubGenerator();

#ifdef _WIN32
    void PatchAppDomainCreate(const ModuleID module_id);
    HRESULT GenerateLoaderMethod(const ModuleID module_id, mdMethodDef* ret_method_token);
#endif

    HRESULT PatchProcessStartupHooks(const ModuleID module_id, const WSTRING& startup_hook_assembly_path);

private:
#ifdef _WIN32
    HRESULT GenerateLoaderType(const ModuleID module_id,
                               mdTypeDef*     loader_type,
                               mdMethodDef*   init_method,
                               mdMethodDef*   patch_app_domain_setup_method);
    HRESULT ModifyAppDomainCreate(const ModuleID module_id, mdMethodDef patch_app_domain_setup_method);
#endif

    HRESULT ModifyProcessStartupHooks(const ModuleID module_id, mdMethodDef patch_startup_hook_method);
    HRESULT GenerateHookFixup(const ModuleID module_id,
                              mdTypeDef*     hook_fixup_type,
                              mdMethodDef*   patch_startup_hook_method,
                              const WSTRING& startup_hook_dll_name);
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_STUB_GENERATOR_H_
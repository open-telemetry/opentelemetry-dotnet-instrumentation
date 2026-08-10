// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

//---------------------------------------------------------------------------------------
// Exports that managed code from OpenTelemetry.AutoInstrumentation.dll will
// P/Invoke into
//
// NOTE: Must keep these signatures in sync with the DllImports in
// NativeMethods.cs!
//---------------------------------------------------------------------------------------

#include "cor_profiler.h"
#include "configuration.h"

#ifndef _WIN32
#include <dlfcn.h>
#endif

#ifdef _WIN32
// GetAssemblyAndSymbolsBytes is used when injecting the Loader into a .NET Framework application.
EXTERN_C VOID STDAPICALLTYPE GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray,
                                                        int*   assemblySize,
                                                        BYTE** pSymbolsArray,
                                                        int*   symbolsSize)
{
    return trace::profiler->GetAssemblyAndSymbolsBytes(pAssemblyArray, assemblySize, pSymbolsArray, symbolsSize);
}
#endif

EXTERN_C BOOL STDAPICALLTYPE IsProfilerAttached()
{
    return trace::profiler != nullptr && trace::profiler->IsAttached();
}

EXTERN_C VOID STDAPICALLTYPE AddInstrumentations(WCHAR* id, trace::CallTargetDefinition* items, int size)
{
    return trace::profiler->AddInstrumentations(id, items, size);
}

EXTERN_C VOID STDAPICALLTYPE AddDerivedInstrumentations(WCHAR* id, trace::CallTargetDefinition* items, int size)
{
    return trace::profiler->AddDerivedInstrumentations(id, items, size);
}

EXTERN_C VOID STDAPICALLTYPE SetSqlClientNetFxILRewriteEnabled(bool enabled)
{
    return trace::SetSqlClientNetFxILRewriteEnabled(enabled);
}

EXTERN_C BOOL STDAPICALLTYPE ConfigureContinuousProfiler(bool         threadSamplingEnabled,
                                                         unsigned int threadSamplingInterval,
                                                         bool         threadSamplingExportPipelinePrepared,
                                                         bool         allocationSamplingEnabled,
                                                         unsigned int maxMemorySamplesPerMinute,
                                                         bool         allocationSamplingExportPipelinePrepared,
                                                         unsigned int selectedThreadSamplingInterval)
{
    return trace::profiler != nullptr &&
           trace::profiler->ConfigureContinuousProfiler(threadSamplingEnabled, threadSamplingInterval,
                                                        threadSamplingExportPipelinePrepared, allocationSamplingEnabled,
                                                        maxMemorySamplesPerMinute,
                                                        allocationSamplingExportPipelinePrepared,
                                                        selectedThreadSamplingInterval);
}

EXTERN_C BOOL STDAPICALLTYPE SetContinuousProfilerSamplingInterval(unsigned int threadSamplingInterval)
{
    return trace::profiler != nullptr && trace::profiler->SetContinuousProfilerSamplingInterval(threadSamplingInterval);
}

EXTERN_C BOOL STDAPICALLTYPE SetContinuousProfilerEnabled(bool enabled)
{
    return trace::profiler != nullptr && trace::profiler->SetContinuousProfilerEnabled(enabled);
}

EXTERN_C unsigned int STDAPICALLTYPE GetContinuousProfilerSamplingInterval()
{
    return trace::profiler == nullptr ? 0 : trace::profiler->GetContinuousProfilerSamplingInterval();
}

EXTERN_C unsigned int STDAPICALLTYPE GetContinuousProfilerAllocationSamplingRate()
{
    return trace::profiler == nullptr ? 0 : trace::profiler->GetContinuousProfilerAllocationSamplingRate();
}

EXTERN_C VOID STDAPICALLTYPE InitializeTraceMethods(WCHAR* id,
                                                    WCHAR* integration_assembly_name_ptr,
                                                    WCHAR* integration_type_name_ptr,
                                                    WCHAR* configuration_string_ptr)
{
    return trace::profiler->InitializeTraceMethods(id, integration_assembly_name_ptr, integration_type_name_ptr,
                                                   configuration_string_ptr);
}

#ifndef _WIN32
EXTERN_C void* dddlopen(const char* __file, int __mode)
{
    return dlopen(__file, __mode);
}

EXTERN_C char* dddlerror(void)
{
    return dlerror();
}

EXTERN_C void* dddlsym(void* __restrict __handle, const char* __restrict __name)
{
    return dlsym(__handle, __name);
}
#endif

#ifdef _WIN32
EXTERN_C INT32 STDAPICALLTYPE GetNetFrameworkRedirectionVersion()
{
    return trace::profiler != nullptr ? trace::profiler->GetNetFrameworkRedirectionVersion() : 0;
}
#endif

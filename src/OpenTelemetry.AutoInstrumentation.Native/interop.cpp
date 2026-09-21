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

EXTERN_C VOID STDAPICALLTYPE ConfigureContinuousProfiler(bool         threadSamplingEnabled,
                                                         unsigned int threadSamplingInterval,
                                                         bool         allocationSamplingEnabled,
                                                         unsigned int maxMemorySamplesPerMinute,
                                                         unsigned int selectedThreadSamplingInterval)
{
    try
    {
        return trace::profiler->ConfigureContinuousProfiler(threadSamplingEnabled, threadSamplingInterval,
                                                            allocationSamplingEnabled, maxMemorySamplesPerMinute,
                                                            selectedThreadSamplingInterval);
    }
    catch (...)
    {
        return;
    }
}

EXTERN_C INT32 STDAPICALLTYPE
ApplyContinuousProfilerConfiguration(const continuous_profiler::RuntimeSamplerConfiguration* request,
                                     const continuous_profiler::RuntimeSamplerAuthority      authority,
                                     continuous_profiler::RuntimeSamplerState*               actualState)
{
    try
    {
        if (trace::profiler == nullptr)
        {
            const auto stateResult = continuous_profiler::EncodeRuntimeSamplerState({}, actualState);
            if (stateResult == continuous_profiler::RuntimeSamplerStateQueryResult::InvalidArgument)
            {
                return static_cast<INT32>(continuous_profiler::RuntimeSamplerApplyResult::RejectedInvalidArgument);
            }
            if (stateResult == continuous_profiler::RuntimeSamplerStateQueryResult::UnsupportedLayout)
            {
                return static_cast<INT32>(continuous_profiler::RuntimeSamplerApplyResult::RejectedUnsupportedLayout);
            }
            return static_cast<INT32>(continuous_profiler::RuntimeSamplerApplyResult::ShuttingDown);
        }

        return static_cast<INT32>(
            trace::profiler->ApplyContinuousProfilerConfiguration(request, authority, actualState));
    }
    catch (...)
    {
        try
        {
            if (trace::profiler != nullptr)
            {
                trace::profiler->GetContinuousProfilerState(actualState);
            }
            else
            {
                continuous_profiler::EncodeRuntimeSamplerState({}, actualState);
            }
        }
        catch (...)
        {
        }
        return static_cast<INT32>(continuous_profiler::RuntimeSamplerApplyResult::ActivationFailed);
    }
}

EXTERN_C INT32 STDAPICALLTYPE GetContinuousProfilerState(continuous_profiler::RuntimeSamplerState* actualState)
{
    try
    {
        if (trace::profiler == nullptr)
        {
            return static_cast<INT32>(continuous_profiler::EncodeRuntimeSamplerState({}, actualState));
        }

        return static_cast<INT32>(trace::profiler->GetContinuousProfilerState(actualState));
    }
    catch (...)
    {
        return static_cast<INT32>(continuous_profiler::RuntimeSamplerStateQueryResult::InvalidArgument);
    }
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

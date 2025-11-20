/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_COR_PROFILER_H_
#define OTEL_CLR_PROFILER_COR_PROFILER_H_

#include "cor.h"
#include "corprof.h"
#include <atomic>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#include "cor_profiler_base.h"
#include "environment_variables.h"
#include "il_rewriter.h"
#include "integration.h"
#include "pal.h"
#include "rejit_preprocessor.h"
#include "rejit_handler.h"
#include <unordered_set>
#include "clr_helpers.h"
#include "stack_capture_strategy.h"

// Forward declaration
namespace continuous_profiler
{
class ContinuousProfiler;
}

namespace trace
{

class CorProfiler : public CorProfilerBase
{
private:
    std::atomic_bool is_attached_ = {false};
    RuntimeInformation runtime_information_;
    std::vector<IntegrationDefinition> integration_definitions_;

    std::unordered_set<WSTRING> definitions_ids_;
    std::mutex definitions_ids_lock_;

    // Startup helper variables
    WSTRING home_path;
    bool first_jit_compilation_completed = false;
    bool startup_fix_required = false;

    bool corlib_module_loaded = false;
    AppDomainID corlib_app_domain_id = 0;
    bool managed_profiler_loaded_domain_neutral = false;
    std::unordered_set<AppDomainID> managed_profiler_loaded_app_domains;
    std::unordered_set<AppDomainID> first_jit_compilation_app_domains;
    bool in_azure_app_services = false;
    bool is_desktop_iis = false;

    continuous_profiler::ContinuousProfiler* continuousProfiler;
    std::unique_ptr<continuous_profiler::IStackCaptureStrategy> stack_capture_strategy_;
    HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;


    //
    // CallTarget Members
    //
    // The variables 'enable_by_ref_instrumentation' and 'enable_calltarget_state_by_ref' will always be true,
    // but instead of removing them and the conditional branches they affect, we will keep the variables to make
    // future upstream pulls easier.
    std::shared_ptr<RejitHandler> rejit_handler = nullptr;
    bool enable_by_ref_instrumentation = true;
    bool enable_calltarget_state_by_ref = true;
    std::unique_ptr<TracerRejitPreprocessor> tracer_integration_preprocessor = nullptr;

    // Cor assembly properties
    AssemblyProperty corAssemblyProperty{};
    AssemblyReference* managed_profiler_assembly_reference;

    //
    // OpCodes helper
    //
    std::vector<std::string> opcodes_names;

    //
    // Module helper variables
    //
    std::mutex module_ids_lock_;
    std::vector<ModuleID> module_ids_;

    //
    // Methods only for .NET Framework
    //
#ifdef _WIN32
    //
    // Special handler for JITCompilationStarted on .NET Framework.
    //
    HRESULT STDMETHODCALLTYPE JITCompilationStartedOnNetFramework(FunctionID function_id, BOOL is_safe_to_block);

    //
    // Assembly redirect private members.
    //
    std::unordered_map<int, std::unordered_map<WSTRING, AssemblyVersionRedirection>> assembly_version_redirect_map_;
    std::unordered_map<WSTRING, AssemblyVersionRedirection>* assembly_version_redirect_map_current_framework_;
    int                                                      assembly_version_redirect_map_current_framework_key_ = 0;

    void InitNetFxAssemblyRedirectsMap();
    void RedirectAssemblyReferences(
        const ComPtr<IMetaDataAssemblyImport>& assembly_import,
        const ComPtr<IMetaDataAssemblyEmit>& assembly_emit);

    //
    // Loader methods. These are only used on the .NET Framework.
    //
    HRESULT RunAutoInstrumentationLoader(const ComPtr<IMetaDataEmit2>&, const ModuleID module_id, const mdToken function_token, const FunctionInfo& caller, const ModuleMetadata& module_metadata);
    HRESULT GenerateLoaderMethod(const ModuleID module_id, mdMethodDef* ret_method_token);
    HRESULT GenerateLoaderType(const ModuleID module_id,
                               mdTypeDef*     loader_type,
                               mdMethodDef*   init_method,
                               mdMethodDef*   patch_app_domain_setup_method);
    HRESULT ModifyAppDomainCreate(const ModuleID module_id, mdMethodDef patch_app_domain_setup_method);
    HRESULT AddIISPreStartInitFlags(const ModuleID module_id, const mdToken function_token);
    void DetectFrameworkVersionTableForRedirectsMap();
#endif

    //
    // Helper methods
    //
    void RewritingPInvokeMaps(const ModuleMetadata& module_metadata, const WSTRING& nativemethods_type_name);
    bool GetIntegrationTypeRef(ModuleMetadata& module_metadata, ModuleID module_id,
                               const IntegrationDefinition& integration_definition, mdTypeRef& integration_type_ref);
    bool ProfilerAssemblyIsLoadedIntoAppDomain(AppDomainID app_domain_id);

    //
    // Initialization methods
    //
    void InternalAddInstrumentation(WCHAR* id, CallTargetDefinition* items, int size, bool isDerived);
    bool InitThreadSampler();

public:
    CorProfiler() = default;

    bool IsAttached() const;

    WSTRING GetBytecodeInstrumentationAssembly() const;

#ifdef _WIN32
    // GetAssemblyAndSymbolsBytes is used when injecting the Loader into a .NET Framework application.
    void GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize, BYTE** pSymbolsArray,
                                    int* symbolsSize) const;

    // Return redirection table used in runtime
    // that will match TFM folder to load assemblies.
    // It may not be actual .NET Framework version.
    int  GetNetFrameworkRedirectionVersion() const;
#endif

    std::string GetILCodes(const std::string&              title,
                           ILRewriter*                     rewriter,
                           const FunctionInfo&             caller,
                           const ComPtr<IMetaDataImport2>& metadata_import);

    //
    // ICorProfilerCallback methods
    //
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* cor_profiler_info_unknown) override;

    HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assembly_id, HRESULT hr_status) override;

    HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID module_id, HRESULT hr_status) override;

    HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID module_id) override;

#ifdef _WIN32
    // JITCompilationStarted is only needed on .NET Framework, see JITCompilationStartedOnNetFramework.
    HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID function_id, BOOL is_safe_to_block) override;
#endif

    HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus) override;

    HRESULT STDMETHODCALLTYPE Shutdown() override;

    HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded() override;

    HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override;
    //
    // ReJIT Methods
    //

    HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId,
                                                      BOOL fIsSafeToBlock) override;

    HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                                                 ICorProfilerFunctionControl* pFunctionControl) override;

    HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus,
                                                       BOOL fIsSafeToBlock) override;

    HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId,
                                         HRESULT hrStatus) override;

    HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction) override;

    // ICorProfilerInfo callbacks to track thread naming (used by ThreadSampler only)
    HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;

    // Needed for allocation sampling
    HRESULT STDMETHODCALLTYPE EventPipeEventDelivered(EVENTPIPE_PROVIDER provider,
                                                      DWORD              eventId,
                                                      DWORD              eventVersion,
                                                      ULONG              cbMetadataBlob,
                                                      LPCBYTE            metadataBlob,
                                                      ULONG              cbEventData,
                                                      LPCBYTE            eventData,
                                                      LPCGUID            pActivityId,
                                                      LPCGUID            pRelatedActivityId,
                                                      ThreadID           eventThread,
                                                      ULONG              numStackFrames,
                                                      UINT_PTR           stackFrames[]) override;

    //
    // ICorProfilerCallback6 methods
    //
    HRESULT STDMETHODCALLTYPE GetAssemblyReferences(const WCHAR* wszAssemblyPath,
                                                    ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;

    //
    // Add Integrations methods
    //
    void AddInstrumentations(WCHAR* id, CallTargetDefinition* items, int size);
    void AddDerivedInstrumentations(WCHAR* id, CallTargetDefinition* items, int size);
    void InitializeTraceMethods(WCHAR* id,
                                WCHAR* integration_assembly_name_ptr,
                                WCHAR* integration_type_name_ptr,
                                WCHAR* configuration_string_ptr);
    //
    // Continuous Profiler methods
    //
    void ConfigureContinuousProfiler(bool threadSamplingEnabled, unsigned int threadSamplingInterval, bool allocationSamplingEnabled, unsigned int maxMemorySamplesPerMinute, unsigned int selectedThreadsSamplingInterval);

    //
    // IL Rewriting methods
    //
    HRESULT RewriteILSystemDataCommandText(const ModuleID module_id);

    friend class TracerMethodRewriter;
};

// Note: Generally you should not have a single, global callback implementation,
// as that prevents your profiler from analyzing multiply loaded in-process
// side-by-side CLRs. However, this profiler implements the "profile-first"
// alternative of dealing with multiple in-process side-by-side CLR instances.
// First CLR to try to load us into this process wins; so there can only be one
// callback implementation created. (See ProfilerCallback::CreateObject.)
extern CorProfiler* profiler; // global reference to callback object

} // namespace trace

#endif // OTEL_CLR_PROFILER_COR_PROFILER_H_
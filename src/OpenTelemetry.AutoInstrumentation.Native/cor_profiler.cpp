// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "cor_profiler.h"

#include "corhlpr.h"
#include <corprof.h>
#include <string>
#include <typeinfo>
#ifdef _WIN32
#include <regex>
#else
#include <re2/re2.h>
#endif

#include "clr_helpers.h"
#include "dllmain.h"
#include "environment_variables.h"
#include "environment_variables_util.h"
#include "il_rewriter.h"
#include "il_rewriter_wrapper.h"
#include "logger.h"
#include "metadata_builder.h"
#include "module_metadata.h"
#include "otel_profiler_constants.h"
#include "pal.h"
#include "resource.h"
#include "startup_hook.h"
#include "stats.h"
#include "util.h"
#include "version.h"
#include "continuous_profiler.h"

#ifdef MACOS
#include <mach-o/dyld.h>
#include <mach-o/getsect.h>
#endif

using namespace std::chrono_literals;

#ifdef _WIN32
#include "netfx_assembly_redirection.h"
#endif

#define FailProfiler(LEVEL, MESSAGE)                                                                                   \
    Logger::LEVEL(MESSAGE);                                                                                            \
    if (IsFailFastEnabled())                                                                                           \
    {                                                                                                                  \
        throw std::runtime_error(MESSAGE);                                                                             \
    }                                                                                                                  \
    else                                                                                                               \
    {                                                                                                                  \
        return E_FAIL;                                                                                                 \
    }

namespace trace
{

CorProfiler* profiler = nullptr;

//
// ICorProfilerCallback methods
//
HRESULT STDMETHODCALLTYPE CorProfiler::Initialize(IUnknown* cor_profiler_info_unknown)
{
    auto _                   = trace::Stats::Instance()->InitializeMeasure();
    this->continuousProfiler = nullptr;

    CorProfilerBase::Initialize(cor_profiler_info_unknown);

    if (Logger::IsDebugEnabled())
    {
        const auto env_variables = GetEnvironmentVariables(env_vars_prefixes_to_display);
        Logger::Debug("Environment variables:");

        // Update the list also in SmokeTests.NativeLogsHaveNoSensitiveData
        const auto secrets_pattern = "(?:^|_)(API|TOKEN|SECRET|KEY|PASSWORD|PASS|PWD|HEADER|CREDENTIALS)(?:_|$)";
#ifdef _WIN32
        const std::regex secrets_regex(secrets_pattern, std::regex_constants::ECMAScript | std::regex_constants::icase);
#else
        static re2::RE2 re(secrets_pattern, RE2::Quiet);
#endif

        for (const auto& env_variable : env_variables)
        {
#ifdef _WIN32
            if (!std::regex_search(ToString(env_variable), secrets_regex))
#else
            if (!re2::RE2::PartialMatch(ToString(env_variable), re))
#endif
            {
                Logger::Debug("  ", env_variable);
            }
            else
            {
                // Remove secret value and replace with <hidden>
                Logger::Debug("  ", env_variable.substr(0, env_variable.find_first_of('=')), "=<hidden>");
            }
        }
    }

    // get ICorProfilerInfo12 for >= .NET 5.0
    ICorProfilerInfo12* info12 = nullptr;
    HRESULT             hr = cor_profiler_info_unknown->QueryInterface(__uuidof(ICorProfilerInfo12), (void**)&info12);
    if (SUCCEEDED(hr))
    {
        Logger::Debug("Interface ICorProfilerInfo12 found.");
        this->info_   = info12;
        this->info12_ = info12;
    }
    else
    {
        // get ICorProfilerInfo7 interface for .NET Framework >= 4.6.1 and any .NET (Core)
        hr = cor_profiler_info_unknown->QueryInterface(__uuidof(ICorProfilerInfo7), (void**)&this->info_);
        if (FAILED(hr))
        {
            FailProfiler(Warn, "Failed to attach profiler: Not supported .NET Framework version (lower than 4.6.1).")
        }
        info12        = nullptr;
        this->info12_ = nullptr;
    }

    // code is ready to get runtime information
    runtime_information_ = GetRuntimeInformation(this->info_);
    if (Logger::IsDebugEnabled())
    {
        if (runtime_information_.is_desktop())
        {
            // on .NET Framework it is the CLR version therefore major_version == 4 and minor_version == 0
            Logger::Debug(".NET Runtime: .NET Framework");
        }
        else if (runtime_information_.major_version < 5)
        {
            // on .NET Core the major_version == 4 and minor_version == 0 (sic!)
            Logger::Debug(".NET Runtime: .NET Core");
        }
        else
        {
            Logger::Debug(".NET Runtime: .NET ", runtime_information_.major_version, ".",
                          runtime_information_.minor_version);
        }
    }

    if (runtime_information_.is_core() && runtime_information_.major_version < 6)
    {
        FailProfiler(Warn, "Failed to attach profiler: Not supported .NET version (lower than 6.0).")
    }

#ifdef _WIN32
    if (runtime_information_.is_desktop() && IsNetFxAssemblyRedirectionEnabled())
    {
        InitNetFxAssemblyRedirectsMap();
    }
#endif

    const auto& process_name          = GetCurrentProcessName();
    const auto& exclude_process_names = GetEnvironmentValues(environment::exclude_process_names);

    // attach profiler only if this process's name is NOT on the list
    if (!exclude_process_names.empty() && Contains(exclude_process_names, process_name))
    {
        Logger::Info("Profiler disabled: ", process_name, " found in ", environment::exclude_process_names, ".");
        FailProfiler(Info, "Profiler disabled - excluded process")
    }

    if (runtime_information_.is_core())
    {
        // .NET Core applications should use the dotnet StartupHook to bootstrap OpenTelemetry so that the
        // necessary dependencies will be available. Bootstrapping with the profiling APIs occurs too early
        // and the necessary dependencies are not available yet.

        // Ensure that OTel StartupHook is listed.
        const auto home_path     = GetEnvironmentValue(environment::profiler_home_path);
        const auto startup_hooks = GetEnvironmentValues(environment::dotnet_startup_hooks, ENV_VAR_PATH_SEPARATOR);
        if (!IsStartupHookValid(startup_hooks, home_path))
        {
            FailProfiler(Error, "The required StartupHook was not configured correctly. No telemetry will be captured.")
        }
    }

    if (IsAzureAppServices())
    {
        Logger::Info("Profiler is operating within Azure App Services context.");
        in_azure_app_services = true;

        const auto& app_pool_id_value = GetEnvironmentValue(environment::azure_app_services_app_pool_id);

        if (app_pool_id_value.size() > 1 && app_pool_id_value.at(0) == '~')
        {
            Logger::Info("Profiler disabled: ", environment::azure_app_services_app_pool_id, " ", app_pool_id_value,
                         " is recognized as an Azure App Services infrastructure process.");
            FailProfiler(Info, "Profiler disabled - Azure App Services infrastructure process.")
        }

        const auto& cli_telemetry_profile_value =
            GetEnvironmentValue(environment::azure_app_services_cli_telemetry_profile_value);

        if (cli_telemetry_profile_value == WStr("AzureKudu"))
        {
            Logger::Info("Profiler disabled: ", app_pool_id_value,
                         " is recognized as Kudu, an Azure App Services reserved process.");
            FailProfiler(Info, "Profiler disabled: - Kudu, an Azure App Services reserved process.")
        }
    }

    auto work_offloader = std::make_shared<RejitWorkOffloader>(this->info_);

    rejit_handler = info12 != nullptr ? std::make_shared<RejitHandler>(info12, work_offloader)
                                      : std::make_shared<RejitHandler>(this->info_, work_offloader);
    tracer_integration_preprocessor = std::make_unique<TracerRejitPreprocessor>(rejit_handler, work_offloader);

    DWORD event_mask = COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST | COR_PRF_MONITOR_MODULE_LOADS |
                       COR_PRF_MONITOR_ASSEMBLY_LOADS | COR_PRF_MONITOR_APPDOMAIN_LOADS | COR_PRF_ENABLE_REJIT;

#ifdef _WIN32
    if (runtime_information_.is_desktop())
    {
        // Only on .NET Framework callbacks for JIT compilation are needed.
        event_mask |= COR_PRF_MONITOR_JIT_COMPILATION;
    }
#endif

    if (!EnableInlining())
    {
        Logger::Info("JIT Inlining is disabled.");
        event_mask |= COR_PRF_DISABLE_INLINING;
    }
    else
    {
        Logger::Info("JIT Inlining is enabled.");
    }

    if (DisableOptimizations())
    {
        Logger::Info("Disabling all code optimizations.");
        event_mask |= COR_PRF_DISABLE_OPTIMIZATIONS;
    }

    if (IsNGENEnabled())
    {
        Logger::Info("NGEN is enabled.");
        event_mask |= COR_PRF_MONITOR_CACHE_SEARCHES;
    }
    else
    {
        Logger::Info("NGEN is disabled.");
        event_mask |= COR_PRF_DISABLE_ALL_NGEN_IMAGES;
    }

    // set event mask to subscribe to events and disable NGEN images
    hr = this->info_->SetEventMask2(event_mask, COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES);
    if (FAILED(hr))
    {
        FailProfiler(Warn, "Failed to attach profiler: unable to set event mask.")
    }

    runtime_information_ = GetRuntimeInformation(this->info_);
    if (process_name == WStr("w3wp.exe") || process_name == WStr("iisexpress.exe"))
    {
        is_desktop_iis = runtime_information_.is_desktop();
    }

    // writing opcodes vector for the IL dumper
    if (IsDumpILRewriteEnabled())
    {
#define OPDEF(c, s, pop, push, args, type, l, s1, s2, flow) opcodes_names.push_back(s);
#include "opcode.def"
#undef OPDEF
        opcodes_names.push_back("(count)"); // CEE_COUNT
        opcodes_names.push_back("->");      // CEE_SWITCH_ARG
    }

    managed_profiler_assembly_reference = AssemblyReference::GetFromCache(GetBytecodeInstrumentationAssembly());

    const auto currentModuleFileName = GetCurrentModuleFileName();
    if (currentModuleFileName == EmptyWStr)
    {
        FailProfiler(Error, "Profiler filepath: cannot be calculated.")
    }

    // we're in!
    Logger::Info("Profiler filepath: ", currentModuleFileName);
    Logger::Info("Profiler attached.");
    this->info_->AddRef();
    is_attached_.store(true);
    profiler = this;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AssemblyLoadFinished(AssemblyID assembly_id, HRESULT hr_status)
{
    auto _ = trace::Stats::Instance()->AssemblyLoadFinishedMeasure();

    if (FAILED(hr_status))
    {
        // if assembly failed to load, skip it entirely,
        // otherwise we can crash the process if module is not valid
        Logger::Warn("AssemblyLoadFinished: ", assembly_id, " ", hr_status);
        CorProfilerBase::AssemblyLoadFinished(assembly_id, hr_status);
        return S_OK;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("AssemblyLoadFinished: ", assembly_id, " ", hr_status);
    }

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_)
    {
        return S_OK;
    }

    const auto& assembly_info = GetAssemblyInfo(this->info_, assembly_id);
    if (!assembly_info.IsValid())
    {
        return S_OK;
    }

    const auto& is_instrumentation_assembly = assembly_info.name == managed_profiler_name;

    if (is_instrumentation_assembly)
    {
        if (Logger::IsDebugEnabled())
        {
            Logger::Debug("AssemblyLoadFinished: Bytecode Instrumentation Assembly: ",
                          GetBytecodeInstrumentationAssembly());
        }

        ComPtr<IUnknown> metadata_interfaces;
        auto             hr = this->info_->GetModuleMetaData(assembly_info.manifest_module_id, ofRead | ofWrite,
                                                 IID_IMetaDataImport2, metadata_interfaces.GetAddressOf());
        if (FAILED(hr))
        {
            Logger::Warn("AssemblyLoadFinished failed to get metadata interface for module id ",
                         assembly_info.manifest_module_id, " from assembly ", assembly_info.name);
            return S_OK;
        }

        // Get the IMetaDataAssemblyImport interface to get metadata from the managed assembly
        const auto& assembly_import   = metadata_interfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
        const auto& assembly_metadata = GetAssemblyImportMetadata(assembly_import);

        managed_profiler_loaded_app_domains.insert(assembly_info.app_domain_id);

        if (runtime_information_.is_desktop() && corlib_module_loaded)
        {
            // Set the managed_profiler_loaded_domain_neutral flag whenever the
            // managed profiler is loaded shared
            if (assembly_info.app_domain_id == corlib_app_domain_id)
            {
                Logger::Info("AssemblyLoadFinished: ", assembly_info.name, " was loaded domain-neutral");
                managed_profiler_loaded_domain_neutral = true;
            }
            else
            {
                Logger::Info("AssemblyLoadFinished: ", assembly_info.name, " was not loaded domain-neutral");
            }
        }
    }

    return S_OK;
}

#ifdef _WIN32
void CorProfiler::RedirectAssemblyReferences(const ComPtr<IMetaDataAssemblyImport>& assembly_import,
                                             const ComPtr<IMetaDataAssemblyEmit>&   assembly_emit)
{
    HRESULT       hr               = S_FALSE;
    HCORENUM      core_enum_handle = NULL;
    const ULONG   assembly_refs_sz = 16;
    mdAssemblyRef assembly_refs[assembly_refs_sz];
    ULONG         assembly_refs_count;

    // Inspect all assembly references and make any necessary redirects.
    while (true)
    {
        hr = assembly_import.Get()->EnumAssemblyRefs(&core_enum_handle, assembly_refs, assembly_refs_sz,
                                                     &assembly_refs_count);
        if (hr == S_FALSE)
        {
            // This is expected when the enumeration finished.
            Logger::Debug("RedirectAssemblyReferences: EnumAssemblyRefs returned S_FALSE assembly_refs_count=",
                          assembly_refs_count);
            break;
        }

        // Loop and process each AssemblyRef
        for (ULONG i = 0; i < assembly_refs_count; i++)
        {
            const void*      public_key_or_token;
            ULONG            public_key_or_token_sz;
            WCHAR            name[kNameMaxSize];
            ULONG            name_len = 0;
            ASSEMBLYMETADATA assembly_metadata{};
            const void*      hash_value;
            ULONG            hash_value_sz;
            DWORD            assembly_flags = 0;

            hr = assembly_import->GetAssemblyRefProps(assembly_refs[i], &public_key_or_token, &public_key_or_token_sz,
                                                      name, kNameMaxSize, &name_len, &assembly_metadata, &hash_value,
                                                      &hash_value_sz, &assembly_flags);
            if (FAILED(hr) || name_len == 0)
            {
                Logger::Warn("RedirectAssemblyReferences: GetAssemblyRefProps failed HRESULT=", HResultStr(hr));
                continue;
            }

            const auto wsz_name = WSTRING(name);
            if (Logger::IsDebugEnabled())
            {
                Logger::Debug("RedirectAssemblyReferences: AssemblyRef for [", wsz_name, "] version=",
                              AssemblyVersionStr(assembly_metadata));
            }

            const auto found_redirect = assembly_version_redirect_map_.find(wsz_name);
            if (found_redirect == assembly_version_redirect_map_.end())
            {
                // No redirection to be applied here.
                continue;
            }

            AssemblyVersionRedirection& redirect           = found_redirect->second;
            auto                        version_comparison = redirect.CompareToAssemblyVersion(assembly_metadata);
            if (version_comparison > 0)
            {
                // Redirection was a higher version, let's proceed with the redirection
                Logger::Info("RedirectAssemblyReferences: redirecting [", wsz_name, "] from_version=",
                             AssemblyVersionStr(assembly_metadata), " to_version=", redirect.VersionStr(),
                             " previous_redirects=", redirect.ulRedirectionCount);
                assembly_metadata.usMajorVersion   = redirect.usMajorVersion;
                assembly_metadata.usMinorVersion   = redirect.usMinorVersion;
                assembly_metadata.usBuildNumber    = redirect.usBuildNumber;
                assembly_metadata.usRevisionNumber = redirect.usRevisionNumber;
                hr = assembly_emit.Get()->SetAssemblyRefProps(assembly_refs[i], public_key_or_token,
                                                              public_key_or_token_sz, name, &assembly_metadata,
                                                              hash_value, hash_value_sz, assembly_flags);
                if (hr != S_OK)
                {
                    Logger::Warn("RedirectAssemblyReferences: redirection error: SetAssemblyRefProps HRESULT=",
                                 HResultStr(hr));
                }
                else
                {
                    redirect.ulRedirectionCount++;
                }
            }
            else if (version_comparison == 0)
            {
                // No need to redirect since it is the same assembly version on the ref and on the map
                if (Logger::IsDebugEnabled())
                {
                    Logger::Debug("RedirectAssemblyReferences: same version for [", wsz_name, "] version=",
                                  redirect.VersionStr(), " previous_redirects=", redirect.ulRedirectionCount);
                }
            }
            else
            {
                // Redirection points to a lower version. If no redirection was done yet modify the map to
                // point to the higher version. If redirection was already applied do not redirect and let
                // the runtime handle it.
                if (redirect.ulRedirectionCount == 0)
                {
                    // Redirection was not applied yet use the higher version. Also increment the redirection
                    // count to indicate that this version was already used.
                    Logger::Info("RedirectAssemblyReferences: redirection update for [", wsz_name, "] to_version=",
                                 AssemblyVersionStr(assembly_metadata), " previous_version_redirection=",
                                 redirect.VersionStr());
                    redirect.usMajorVersion   = assembly_metadata.usMajorVersion;
                    redirect.usMinorVersion   = assembly_metadata.usMinorVersion;
                    redirect.usBuildNumber    = assembly_metadata.usBuildNumber;
                    redirect.usRevisionNumber = assembly_metadata.usRevisionNumber;
                    redirect.ulRedirectionCount++;
                }
                else
                {
                    // This is risky: we aren't sure if the reference will be actually be used during the runtime.
                    // So it is possible that nothing will happen but we can't be sure. Using higher versions on
                    // the OpenTelemetry.AutoInstrumentation dependencies minimizes the chances of hitting this code
                    // path.
                    Logger::Error("RedirectAssemblyReferences: AssemblyRef [", wsz_name, "] version=",
                                  AssemblyVersionStr(assembly_metadata),
                                  " has a higher version than an earlier applied redirection to version=",
                                  redirect.VersionStr());
                }
            }
        }
    }
}
#endif

void CorProfiler::RewritingPInvokeMaps(const ModuleMetadata& module_metadata, const WSTRING& nativemethods_type_name)
{
    HRESULT     hr;
    const auto& metadata_import = module_metadata.metadata_import;
    const auto& metadata_emit   = module_metadata.metadata_emit;

    // We are in the right module, so we try to load the mdTypeDef from the target type name.
    mdTypeDef nativeMethodsTypeDef = mdTypeDefNil;
    auto      foundType =
        FindTypeDefByName(nativemethods_type_name, module_metadata.assemblyName, metadata_import, nativeMethodsTypeDef);
    if (foundType)
    {
        // Define the actual profiler file path as a ModuleRef
        WSTRING native_profiler_file = GetCurrentModuleFileName();
        Logger::Info("Rewriting PInvokes to native: ", native_profiler_file);

        mdModuleRef profiler_ref;
        hr = metadata_emit->DefineModuleRef(native_profiler_file.c_str(), &profiler_ref);
        if (SUCCEEDED(hr))
        {
            // Enumerate all methods inside the native methods type with the PInvokes
            Enumerator<mdMethodDef> enumMethods = Enumerator<mdMethodDef>(
                [metadata_import, nativeMethodsTypeDef](HCORENUM* ptr, mdMethodDef arr[], ULONG max, ULONG* cnt)
                    -> HRESULT { return metadata_import->EnumMethods(ptr, nativeMethodsTypeDef, arr, max, cnt); },
                [metadata_import](HCORENUM ptr) -> void { metadata_import->CloseEnum(ptr); });

            EnumeratorIterator<mdMethodDef> enumIterator = enumMethods.begin();
            while (enumIterator != enumMethods.end())
            {
                auto methodDef = *enumIterator;

                const auto& caller = GetFunctionInfo(module_metadata.metadata_import, methodDef);
                Logger::Info("Rewriting pinvoke for: ", caller.name);

                // Get the current PInvoke map to extract the flags and the entrypoint name
                DWORD       pdwMappingFlags;
                WCHAR       importName[kNameMaxSize]{};
                DWORD       importNameLength = 0;
                mdModuleRef importModule;
                hr = metadata_import->GetPinvokeMap(methodDef, &pdwMappingFlags, importName, kNameMaxSize,
                                                    &importNameLength, &importModule);
                if (SUCCEEDED(hr))
                {
                    // Delete the current PInvoke map
                    hr = metadata_emit->DeletePinvokeMap(methodDef);
                    if (SUCCEEDED(hr))
                    {
                        // Define a new PInvoke map with the new ModuleRef of the actual profiler file path
                        hr = metadata_emit->DefinePinvokeMap(methodDef, pdwMappingFlags, WSTRING(importName).c_str(),
                                                             profiler_ref);
                        if (FAILED(hr))
                        {
                            Logger::Warn("RewritingPInvokeMaps: DefinePinvokeMap to the actual profiler file path "
                                         "failed, trying to restore the previous one.");
                            hr = metadata_emit->DefinePinvokeMap(methodDef, pdwMappingFlags,
                                                                 WSTRING(importName).c_str(), importModule);
                            if (FAILED(hr))
                            {
                                // We only warn that we cannot rewrite the PInvokeMap but we still continue the module
                                // load.
                                // These errors must be handled on the caller with a try/catch.
                                Logger::Warn("RewritingPInvokeMaps: Error trying to restore the previous PInvokeMap.");
                            }
                        }
                    }
                    else
                    {
                        // We only warn that we cannot rewrite the PInvokeMap but we still continue the module load.
                        // These errors must be handled on the caller with a try/catch.
                        Logger::Warn("RewritingPInvokeMaps: DeletePinvokeMap failed");
                    }
                }

                enumIterator = ++enumIterator;
            }
        }
        else
        {
            // We only warn that we cannot rewrite the PInvokeMap but we still continue the module load.
            // These errors must be handled on the caller with a try/catch.
            Logger::Warn("RewritingPInvokeMaps: Native Profiler DefineModuleRef failed");
        }
    }
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadFinished(ModuleID module_id, HRESULT hr_status)
{
    auto _ = trace::Stats::Instance()->ModuleLoadFinishedMeasure();

    if (FAILED(hr_status))
    {
        // if module failed to load, skip it entirely,
        // otherwise we can crash the process if module is not valid
        CorProfilerBase::ModuleLoadFinished(module_id, hr_status);
        return S_OK;
    }

    if (!is_attached_)
    {
        return S_OK;
    }

    // keep this lock until we are done using the module,
    // to prevent it from unloading while in use
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_ || rejit_handler == nullptr)
    {
        return S_OK;
    }

    const auto& module_info = GetModuleInfo(this->info_, module_id);
    if (!module_info.IsValid())
    {
        return S_OK;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("ModuleLoadFinished: ", module_id, " ", module_info.assembly.name, " AppDomain ",
                      module_info.assembly.app_domain_id, " [", module_info.assembly.app_domain_name, "] ",
                      std::boolalpha, " | IsNGEN = ", module_info.IsNGEN(), " | IsDynamic = ", module_info.IsDynamic(),
                      " | IsResource = ", module_info.IsResource(), std::noboolalpha);
    }

    if (module_info.IsNGEN())
    {
        // We check if the Module contains NGEN images and added to the
        // rejit handler list to verify the inlines.
        rejit_handler->AddNGenInlinerModule(module_id);
    }

    AppDomainID app_domain_id = module_info.assembly.app_domain_id;

    // Identify the AppDomain ID of mscorlib which will be the Shared Domain
    // because mscorlib is always a domain-neutral assembly
    if (!corlib_module_loaded && (module_info.assembly.name == mscorlib_assemblyName ||
                                  module_info.assembly.name == system_private_corelib_assemblyName))
    {
        corlib_module_loaded = true;
        corlib_app_domain_id = app_domain_id;

        ComPtr<IUnknown> metadata_interfaces;
        auto             hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                 metadata_interfaces.GetAddressOf());

        // Get the IMetaDataAssemblyImport interface to get metadata from the
        // managed assembly
        const auto& assembly_import   = metadata_interfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
        const auto& assembly_metadata = GetAssemblyImportMetadata(assembly_import);

        hr = assembly_import->GetAssemblyProps(assembly_metadata.assembly_token, &corAssemblyProperty.ppbPublicKey,
                                               &corAssemblyProperty.pcbPublicKey, &corAssemblyProperty.pulHashAlgId,
                                               NULL, 0, NULL, &corAssemblyProperty.pMetaData,
                                               &corAssemblyProperty.assemblyFlags);

        if (FAILED(hr))
        {
            Logger::Warn("AssemblyLoadFinished failed to get properties for COR assembly ");
        }

        corAssemblyProperty.szName = module_info.assembly.name;

        Logger::Info("COR library: ", corAssemblyProperty.szName, " ", corAssemblyProperty.pMetaData.usMajorVersion,
                     ".", corAssemblyProperty.pMetaData.usMinorVersion, ".",
                     corAssemblyProperty.pMetaData.usRevisionNumber);

        if (rejit_handler != nullptr)
        {
            rejit_handler->SetCorAssemblyProfiler(&corAssemblyProperty);
        }

        return S_OK;
    }

    // In IIS, the OpenTelemetry.AutoInstrumentation will be inserted into a method in System.Web (which is
    // domain-neutral)
    // but the OpenTelemetry.AutoInstrumentation.Loader assembly that the CLR profiler loads from a
    // byte array will be loaded into a non-shared AppDomain.
    // In this case, do not insert another Loader into that non-shared AppDomain
    if (module_info.assembly.name == opentelemetry_autoinstrumentation_loader_assemblyName)
    {
        Logger::Info("ModuleLoadFinished: OpenTelemetry.AutoInstrumentation.Loader loaded into AppDomain ",
                     app_domain_id, " [", module_info.assembly.app_domain_name, "]");
        first_jit_compilation_app_domains.insert(app_domain_id);
        return S_OK;
    }

    if (module_info.IsWindowsRuntime())
    {
        // We cannot obtain writable metadata interfaces on Windows Runtime modules
        // or instrument their IL.
        Logger::Debug("ModuleLoadFinished skipping Windows Metadata module: ", module_id, " ",
                      module_info.assembly.name);
        return S_OK;
    }

    if (module_info.IsResource())
    {
        // We don't need to load metadata on resources modules.
        Logger::Debug("ModuleLoadFinished skipping Resources module: ", module_id, " ", module_info.assembly.name);
        return S_OK;
    }

    if (module_info.IsDynamic())
    {
        // For CallTarget we don't need to load metadata on dynamic modules.
        Logger::Debug("ModuleLoadFinished skipping Dynamic module: ", module_id, " ", module_info.assembly.name);
        return S_OK;
    }

    // It is not safe to skip assemblies if applying redirection on .NET Framework
    if (!runtime_information_.is_desktop() || !IsNetFxAssemblyRedirectionEnabled())
    {
        // Not .NET Framework or assembly redirection is disabled, check if the
        // assembly can be skipped.
        for (auto&& skip_assembly : skip_assemblies)
        {
            if (module_info.assembly.name == skip_assembly)
            {
                Logger::Debug("ModuleLoadFinished skipping known module: ", module_id, " ", module_info.assembly.name);
                return S_OK;
            }
        }

        for (auto&& skip_assembly_pattern : skip_assembly_prefixes)
        {
            if (module_info.assembly.name.rfind(skip_assembly_pattern, 0) == 0)
            {
                Logger::Debug("ModuleLoadFinished skipping module by pattern: ", module_id, " ",
                              module_info.assembly.name);
                return S_OK;
            }
        }
    }

#ifdef _WIN32
    const bool perform_netfx_redirect = runtime_information_.is_desktop() && IsNetFxAssemblyRedirectionEnabled();
#else
    const bool perform_netfx_redirect = false;
#endif // _WIN32

    if (perform_netfx_redirect || module_info.assembly.name == managed_profiler_name)
    {
        ComPtr<IUnknown> metadata_interfaces;
        auto             hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                 metadata_interfaces.GetAddressOf());

        if (FAILED(hr))
        {
            Logger::Warn("ModuleLoadFinished failed to get metadata interface for ", module_id, " ",
                         module_info.assembly.name);
            return S_OK;
        }

        const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
        const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
        const auto& assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
        const auto& assembly_emit   = metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

        const auto& module_metadata =
            ModuleMetadata(metadata_import, metadata_emit, assembly_import, assembly_emit, module_info.assembly.name,
                           module_info.assembly.app_domain_id, &corAssemblyProperty);

#ifdef _WIN32
        if (perform_netfx_redirect)
        {
            // On the .NET Framework redirect any assembly reference to the versions required by
            // OpenTelemetry.AutoInstrumentation assembly, the ones under netfx/ folder.
            RedirectAssemblyReferences(assembly_import, assembly_emit);
        }
#endif // _WIN32

        if (module_info.assembly.name == managed_profiler_name)
        {
#ifdef _WIN32
            RewritingPInvokeMaps(module_metadata, windows_nativemethods_type);
#else
            RewritingPInvokeMaps(module_metadata, nonwindows_nativemethods_type);
#endif // _WIN32
        }

        if (Logger::IsDebugEnabled())
        {
            const auto& assemblyImport  = GetAssemblyImportMetadata(assembly_import);
            const auto& assemblyVersion = assemblyImport.version.str();

            Logger::Debug("ModuleLoadFinished: done ", module_info.assembly.name, " v", assemblyVersion);
        }
    }

    if (module_info.assembly.name != managed_profiler_name)
    {
        module_ids_.push_back(module_id);

        // We call the function to analyze the module and request the ReJIT of integrations defined in this module.
        if (tracer_integration_preprocessor != nullptr && !integration_definitions_.empty())
        {
            std::promise<ULONG> promise;
            std::future<ULONG>  future = promise.get_future();
            tracer_integration_preprocessor->EnqueueRequestRejitForLoadedModules(std::vector<ModuleID>{module_id},
                                                                                 integration_definitions_, &promise);

            // wait and get the value from the future<ULONG>
            const auto status = future.wait_for(100ms);

            if (status != std::future_status::timeout)
            {
                const auto& numReJITs = future.get();
                Logger::Debug("Total number of ReJIT Requested: ", numReJITs);
            }
            else
            {
                Logger::Warn("Timeout while waiting for the rejit requests to be processed. Rejit will continue "
                             "asynchronously, but some initial calls may not be instrumented");
            }
        }
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleUnloadStarted(ModuleID module_id)
{
    auto _ = trace::Stats::Instance()->ModuleUnloadStartedMeasure();

    if (!is_attached_)
    {
        return S_OK;
    }

    if (Logger::IsDebugEnabled())
    {
        const auto module_info = GetModuleInfo(this->info_, module_id);

        if (module_info.IsValid())
        {
            Logger::Debug("ModuleUnloadStarted: ", module_id, " ", module_info.assembly.name, " AppDomain ",
                          module_info.assembly.app_domain_id, " [", module_info.assembly.app_domain_name, "]");
        }
        else
        {
            Logger::Debug("ModuleUnloadStarted: ", module_id);
        }
    }

    // take this lock so we block until the
    // module metadata is not longer being used
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_)
    {
        return S_OK;
    }

    const auto& moduleInfo = GetModuleInfo(this->info_, module_id);

    if (moduleInfo.IsValid())
    {
        if (Logger::IsDebugEnabled())
        {
            Logger::Debug("ModuleUnloadStarted: ", module_id, " ", moduleInfo.assembly.name, " AppDomain ",
                          moduleInfo.assembly.app_domain_id, " ", moduleInfo.assembly.app_domain_name);
        }
    }
    else
    {
        Logger::Debug("ModuleUnloadStarted: ", module_id);
        return S_OK;
    }

    const auto is_instrumentation_assembly = moduleInfo.assembly.name == managed_profiler_name;

    if (is_instrumentation_assembly)
    {
        const auto appDomainId = moduleInfo.assembly.app_domain_id;

        // remove appdomain id from managed_profiler_loaded_app_domains set
        if (managed_profiler_loaded_app_domains.find(appDomainId) != managed_profiler_loaded_app_domains.end())
        {
            managed_profiler_loaded_app_domains.erase(appDomainId);
        }
    }

    if (rejit_handler != nullptr)
    {
        rejit_handler->RemoveModule(module_id);
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::Shutdown()
{
    is_attached_.store(false);

    CorProfilerBase::Shutdown();

    // keep this lock until we are done using the module,
    // to prevent it from unloading while in use
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    if (rejit_handler != nullptr)
    {
        rejit_handler->Shutdown();
        rejit_handler = nullptr;
    }
    Logger::Info("Exiting...");
    Logger::Debug("   ModuleIds: ", module_ids_.size());
    Logger::Debug("   IntegrationDefinitions: ", integration_definitions_.size());
    Logger::Debug("   DefinitionsIds: ", definitions_ids_.size());
    Logger::Debug("   ManagedProfilerLoadedAppDomains: ", managed_profiler_loaded_app_domains.size());
    Logger::Debug("   FirstJitCompilationAppDomains: ", first_jit_compilation_app_domains.size());
    Logger::Info("Stats: ", Stats::Instance()->ToString());
    Logger::Shutdown();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ProfilerDetachSucceeded()
{
    if (!is_attached_)
    {
        return S_OK;
    }

    CorProfilerBase::ProfilerDetachSucceeded();

    // keep this lock until we are done using the module,
    // to prevent it from unloading while in use
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_)
    {
        return S_OK;
    }

    Logger::Info("Detaching profiler.");
    Logger::Flush();
    is_attached_.store(false);
    return S_OK;
}

#ifdef _WIN32
// JITCompilationStarted is only called for .NET Framework. It is used to inject the Loader
// into the application.
HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStarted(FunctionID function_id, BOOL is_safe_to_block)
{
    auto _ = trace::Stats::Instance()->JITCompilationStartedMeasure();

    // The flag for this callback is only set if runtime_information_.is_desktop() is true.
    // So there is no need to check it again here.
    if (is_attached_ && is_safe_to_block)
    {
        // The JIT compilation only needs to be tracked on the .NET Framework so the Loader
        // can be injected. For .NET the DOTNET_STARTUP_HOOK takes care of injecting the
        // instrumentation startup code.
        return JITCompilationStartedOnNetFramework(function_id, is_safe_to_block);
    }

    return S_OK;
}
#endif

HRESULT STDMETHODCALLTYPE CorProfiler::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    if (!is_attached_)
    {
        return S_OK;
    }

    // take this lock so we block until the
    // module metadata is not longer being used
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_)
    {
        return S_OK;
    }

    // remove appdomain metadata from map
    const auto& count = first_jit_compilation_app_domains.erase(appDomainId);

    Logger::Debug("AppDomainShutdownFinished: AppDomain: ", appDomainId, ", removed ", count, " elements");

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline)
{
    auto _ = trace::Stats::Instance()->JITInliningMeasure();

    if (!is_attached_ || rejit_handler == nullptr)
    {
        return S_OK;
    }

    ModuleID calleeModuleId;
    mdToken  calleFunctionToken = mdTokenNil;
    auto     hr                 = this->info_->GetFunctionInfo(calleeId, nullptr, &calleeModuleId, &calleFunctionToken);

    *pfShouldInline = true;

    if (FAILED(hr))
    {
        Logger::Warn("*** JITInlining: Failed to get the function info of the calleId: ", calleeId);
        return S_OK;
    }

    if (is_attached_ && rejit_handler != nullptr &&
        rejit_handler->HasModuleAndMethod(calleeModuleId, calleFunctionToken))
    {
        Logger::Debug("*** JITInlining: Inlining disabled for [ModuleId=", calleeModuleId, ", MethodDef=",
                      TokenStr(&calleFunctionToken), "]");
        *pfShouldInline = false;
    }

    return S_OK;
}

bool CorProfiler::IsAttached() const
{
    return is_attached_;
}

void CorProfiler::AddInstrumentations(WCHAR* id, CallTargetDefinition* items, int size)
{
    auto    _             = trace::Stats::Instance()->InitializeProfilerMeasure();
    WSTRING definitionsId = WSTRING(id);
    Logger::Info("AddInstrumentations: received id: ", definitionsId, " from managed side with ", size,
                 " integrations.");

    if (size > 0)
    {
        InternalAddInstrumentation(id, items, size, false);
    }
}

void CorProfiler::AddDerivedInstrumentations(WCHAR* id, CallTargetDefinition* items, int size)
{
    auto    _             = trace::Stats::Instance()->InitializeProfilerMeasure();
    WSTRING definitionsId = WSTRING(id);
    Logger::Info("AddDerivedInstrumentations: received id: ", definitionsId, " from managed side with ", size,
                 " integrations.");

    if (size > 0)
    {
        InternalAddInstrumentation(id, items, size, true);
    }
}

void CorProfiler::InternalAddInstrumentation(WCHAR* id, CallTargetDefinition* items, int size, bool isDerived)
{
    WSTRING                      definitionsId = WSTRING(id);
    std::scoped_lock<std::mutex> definitionsLock(definitions_ids_lock_);

    if (definitions_ids_.find(definitionsId) != definitions_ids_.end())
    {
        Logger::Info("InternalAddInstrumentation: Id already processed.");
        return;
    }

    if (items != nullptr && rejit_handler != nullptr)
    {
        std::vector<IntegrationDefinition> integrationDefinitions;

        for (int i = 0; i < size; i++)
        {
            const CallTargetDefinition& current = items[i];

            const WSTRING& targetAssembly = WSTRING(current.targetAssembly);
            const WSTRING& targetType     = WSTRING(current.targetType);
            const WSTRING& targetMethod   = WSTRING(current.targetMethod);

            const WSTRING& integrationAssembly = WSTRING(current.integrationAssembly);
            const WSTRING& integrationType     = WSTRING(current.integrationType);

            std::vector<WSTRING> signatureTypes;
            for (int sIdx = 0; sIdx < current.signatureTypesLength; sIdx++)
            {
                const auto& currentSignature = current.signatureTypes[sIdx];
                if (currentSignature != nullptr)
                {
                    signatureTypes.push_back(WSTRING(currentSignature));
                }
            }

            const Version& minVersion =
                Version(current.targetMinimumMajor, current.targetMinimumMinor, current.targetMinimumPatch, 0);
            const Version& maxVersion =
                Version(current.targetMaximumMajor, current.targetMaximumMinor, current.targetMaximumPatch, 0);

            const auto& integration =
                IntegrationDefinition(MethodReference(targetAssembly, targetType, targetMethod, minVersion, maxVersion,
                                                      signatureTypes),
                                      TypeReference(integrationAssembly, integrationType, {}, {}), isDerived);

            if (Logger::IsDebugEnabled())
            {
                Logger::Debug("  * Target: ", targetAssembly, " | ", targetType, ".", targetMethod, "(",
                              signatureTypes.size(), ") { ", minVersion.str(), " - ", maxVersion.str(), " } [",
                              integrationAssembly, " | ", integrationType, "]");
            }

            integrationDefinitions.push_back(integration);
        }

        std::scoped_lock<std::mutex> moduleLock(module_ids_lock_);

        definitions_ids_.emplace(definitionsId);

        Logger::Info("Total number of modules to analyze: ", module_ids_.size());

        if (!integrationDefinitions.empty())
        {
            if (rejit_handler != nullptr)
            {
                std::promise<ULONG> promise;
                std::future<ULONG>  future = promise.get_future();
                tracer_integration_preprocessor->EnqueueRequestRejitForLoadedModules(module_ids_,
                                                                                     integrationDefinitions, &promise);

                // wait and get the value from the future<int>
                const auto& numReJITs = future.get();
                Logger::Debug("Total number of ReJIT Requested: ", numReJITs);
            }

            integration_definitions_.reserve(integration_definitions_.size() + integrationDefinitions.size());
            for (const auto& integration : integrationDefinitions)
            {
                integration_definitions_.push_back(integration);
            }
        }

        Logger::Info("InternalAddInstrumentation: Total integrations in profiler: ", integration_definitions_.size());
    }
}

void CorProfiler::ConfigureContinuousProfiler(bool         threadSamplingEnabled,
                                              unsigned int threadSamplingInterval,
                                              bool         allocationSamplingEnabled,
                                              unsigned int maxMemorySamplesPerMinute)
{
    Logger::Info("ConfigureContinuousProfiler: thread sampling enabled: ", threadSamplingEnabled,
                 ", thread sampling interval: ", threadSamplingInterval, ", allocationSamplingEnabled: ",
                 allocationSamplingEnabled, ", max memory samples per minute: ", maxMemorySamplesPerMinute);

    if (!threadSamplingEnabled && !allocationSamplingEnabled)
    {
        Logger::Debug("ConfigureContinuousProfiler: Thread sampling and allocations sampling disabled.");
        return;
    }

    DWORD pdvEventsLow;
    DWORD pdvEventsHigh;
    auto  hr = this->info12_->GetEventMask2(&pdvEventsLow, &pdvEventsHigh);
    if (FAILED(hr))
    {
        Logger::Warn("ConfigureContinuousProfiler: Failed to take event masks for continuous profiler.");
        return;
    }

    pdvEventsLow |= COR_PRF_MONITOR_THREADS | COR_PRF_ENABLE_STACK_SNAPSHOT;

    hr = this->info12_->SetEventMask2(pdvEventsLow, pdvEventsHigh);
    if (FAILED(hr))
    {
        Logger::Warn("ConfigureContinuousProfiler: Failed to set event masks for continuous profiler.");
        return;
    }

    this->continuousProfiler = new continuous_profiler::ContinuousProfiler();
    this->continuousProfiler->SetGlobalInfo12(this->info12_);
    Logger::Info("ConfigureContinuousProfiler: Events masks configured for continuous profiler");

    if (threadSamplingEnabled)
    {
        this->continuousProfiler->StartThreadSampling(threadSamplingInterval);
    }

    if (allocationSamplingEnabled)
    {
        this->continuousProfiler->StartAllocationSampling(maxMemorySamplesPerMinute);
    }
}

//
// ICorProfilerCallback6 methods
//
HRESULT STDMETHODCALLTYPE CorProfiler::GetAssemblyReferences(const WCHAR*                           wszAssemblyPath,
                                                             ICorProfilerAssemblyReferenceProvider* pAsmRefProvider)
{
    if (IsAzureAppServices())
    {
        Logger::Debug(
            "GetAssemblyReferences skipping entire callback because this is running in Azure App Services, which "
            "isn't yet supported for this feature. AssemblyPath=",
            wszAssemblyPath);
        return S_OK;
    }

    // Convert the assembly path to the assembly name, assuming the assembly name
    // is either <assembly_name.ni.dll> or <assembly_name>.dll
    const auto& assemblyPathString   = ToString(wszAssemblyPath);
    auto        filename             = assemblyPathString.substr(assemblyPathString.find_last_of("\\/") + 1);
    const auto& lastNiDllPeriodIndex = filename.rfind(".ni.dll");
    const auto& lastDllPeriodIndex   = filename.rfind(".dll");
    if (lastNiDllPeriodIndex != std::string::npos)
    {
        filename.erase(lastNiDllPeriodIndex, 7);
    }
    else if (lastDllPeriodIndex != std::string::npos)
    {
        filename.erase(lastDllPeriodIndex, 4);
    }

    const WSTRING& assembly_name = ToWSTRING(filename);

    // Skip known framework assemblies that we will not instrument and,
    // as a result, will not need an assembly reference to the
    // managed profiler
    for (auto&& skip_assembly_pattern : skip_assembly_prefixes)
    {
        if (assembly_name.rfind(skip_assembly_pattern, 0) == 0)
        {
            Logger::Debug("GetAssemblyReferences skipping module by pattern: Name=", assembly_name, " Path=",
                          wszAssemblyPath);
            return S_OK;
        }
    }

    for (auto&& skip_assembly : skip_assemblies)
    {
        if (assembly_name == skip_assembly)
        {
            Logger::Debug("GetAssemblyReferences skipping known assembly: Name=", assembly_name, " Path=",
                          wszAssemblyPath);
            return S_OK;
        }
    }

    // Construct an ASSEMBLYMETADATA structure for the managed profiler that can
    // be consumed by the runtime
    ASSEMBLYMETADATA assembly_metadata{};

    assembly_metadata.usMajorVersion   = managed_profiler_assembly_reference->version.major;
    assembly_metadata.usMinorVersion   = managed_profiler_assembly_reference->version.minor;
    assembly_metadata.usBuildNumber    = managed_profiler_assembly_reference->version.build;
    assembly_metadata.usRevisionNumber = managed_profiler_assembly_reference->version.revision;
    if (managed_profiler_assembly_reference->locale == WStr("neutral"))
    {
        assembly_metadata.szLocale = const_cast<WCHAR*>(WStr("\0"));
        assembly_metadata.cbLocale = 0;
    }
    else
    {
        assembly_metadata.szLocale = const_cast<WCHAR*>(managed_profiler_assembly_reference->locale.c_str());
        assembly_metadata.cbLocale = (DWORD)(managed_profiler_assembly_reference->locale.size());
    }

    DWORD public_key_size = 8;
    if (managed_profiler_assembly_reference->public_key == trace::PublicKey())
    {
        public_key_size = 0;
    }

    COR_PRF_ASSEMBLY_REFERENCE_INFO asmRefInfo;
    asmRefInfo.pbPublicKeyOrToken = (void*)&managed_profiler_assembly_reference->public_key.data[0];
    asmRefInfo.cbPublicKeyOrToken = public_key_size;
    asmRefInfo.szName             = managed_profiler_assembly_reference->name.c_str();
    asmRefInfo.pMetaData          = &assembly_metadata;
    asmRefInfo.pbHashValue        = nullptr;
    asmRefInfo.cbHashValue        = 0;
    asmRefInfo.dwAssemblyRefFlags = 0;

    // Attempt to extend the assembly closure of the provided assembly to include
    // the managed profiler
    auto hr = pAsmRefProvider->AddAssemblyReference(&asmRefInfo);
    if (FAILED(hr))
    {
        Logger::Warn("GetAssemblyReferences failed for call from ", wszAssemblyPath);
        return S_OK;
    }

    Logger::Debug("GetAssemblyReferences extending assembly closure for ", assembly_name, " to include ",
                  asmRefInfo.szName, ". Path=", wszAssemblyPath);

    return S_OK;
}

#ifdef _WIN32
HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStartedOnNetFramework(FunctionID function_id,
                                                                           BOOL       is_safe_to_block)
{
    // keep this lock until we are done using the module,
    // to prevent it from unloading while in use
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // double check if is_attached_ has changed to avoid possible race condition with shutdown function
    if (!is_attached_)
    {
        return S_OK;
    }

    ModuleID module_id;
    mdToken  function_token = mdTokenNil;

    HRESULT hr = this->info_->GetFunctionInfo(function_id, nullptr, &module_id, &function_token);
    if (FAILED(hr))
    {
        Logger::Warn("JITCompilationStarted: Call to ICorProfilerInfo4.GetFunctionInfo() failed for ", function_id);
        return S_OK;
    }

    // we have to check if the Id is in the module_ids_ vector.
    // In case is True we create a local ModuleMetadata to inject the loader.
    if (!Contains(module_ids_, module_id))
    {
        return S_OK;
    }

    // We check if we are in CallTarget mode and the loader was already injected.
    const auto& module_info = GetModuleInfo(this->info_, module_id);

    bool has_loader_injected_in_appdomain =
        first_jit_compilation_app_domains.find(module_info.assembly.app_domain_id) !=
        first_jit_compilation_app_domains.end();

    if (has_loader_injected_in_appdomain)
    {
        // Loader was already injected in a calltarget scenario, we don't need to do anything else here
        return S_OK;
    }

    ComPtr<IUnknown> metadataInterfaces;
    hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                        metadataInterfaces.GetAddressOf());

    const auto& metadataImport = metadataInterfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadataEmit   = metadataInterfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    const auto& assemblyImport = metadataInterfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
    const auto& assemblyEmit   = metadataInterfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

    Logger::Debug("Temporaly allocating the ModuleMetadata for injection. ModuleId=", module_id, " ModuleName=",
                  module_info.assembly.name);

    std::unique_ptr<ModuleMetadata> module_metadata =
        std::make_unique<ModuleMetadata>(metadataImport, metadataEmit, assemblyImport, assemblyEmit,
                                         module_info.assembly.name, module_info.assembly.app_domain_id,
                                         &corAssemblyProperty);

    // get function info
    const auto& caller = GetFunctionInfo(module_metadata->metadata_import, function_token);
    if (!caller.IsValid())
    {
        return S_OK;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("JITCompilationStarted: function_id=", function_id, " token=", function_token, " name=",
                      caller.type.name, ".", caller.name, "()");
    }

    // IIS: Ensure that the OpenTelemetry.AutoInstrumentation assembly is inserted into
    // System.Web.Compilation.BuildManager.InvokePreStartInitMethods.
    // This will be the first call-site considered for the injection,
    // which correctly loads OpenTelemetry.AutoInstrumentation.Loader into the application's
    // own AppDomain because at this point in the code path, the ApplicationImpersonationContext
    // has been started.
    //
    // Note: This check must only run on desktop because it is possible (and the default) to host
    // ASP.NET Core in-process, so a new .NET Core runtime is instantiated and run in the same w3wp.exe process
    auto valid_loader_callsite = true;
    if (is_desktop_iis)
    {
        valid_loader_callsite = module_metadata->assemblyName == WStr("System.Web") &&
                                caller.type.name == WStr("System.Web.Compilation.BuildManager") &&
                                caller.name == WStr("InvokePreStartInitMethods");
    }
    else if (module_metadata->assemblyName == WStr("System") ||
             module_metadata->assemblyName == WStr("System.Net.Http"))
    {
        valid_loader_callsite = false;
    }

    // The first time a method is JIT compiled in an AppDomain, insert our Loader,
    // which, at a minimum, must add an AssemblyResolve event so we can find
    // OpenTelemetry.AutoInstrumentation.dll and its dependencies on disk.
    if (valid_loader_callsite && !has_loader_injected_in_appdomain)
    {
        bool domain_neutral_assembly = runtime_information_.is_desktop() && corlib_module_loaded &&
                                       module_metadata->app_domain_id == corlib_app_domain_id;
        Logger::Info("JITCompilationStarted: Startup hook registered in function_id=", function_id, " token=",
                     function_token, " name=", caller.type.name, ".", caller.name, "(), assembly_name=",
                     module_metadata->assemblyName, " app_domain_id=", module_metadata->app_domain_id,
                     " domain_neutral=", domain_neutral_assembly);

        first_jit_compilation_app_domains.insert(module_metadata->app_domain_id);

        hr = RunAutoInstrumentationLoader(module_metadata->metadata_emit, module_id, function_token, caller,
                                          *module_metadata);
        if (FAILED(hr))
        {
            Logger::Warn("JITCompilationStarted: Call to RunAutoInstrumentationLoader() failed for ", module_id, " ",
                         function_token);
            return S_OK;
        }

        if (is_desktop_iis)
        {
            hr = AddIISPreStartInitFlags(module_id, function_token);
            if (FAILED(hr))
            {
                Logger::Warn("JITCompilationStarted: Call to AddIISPreStartInitFlags() failed for ", module_id, " ",
                             function_token);
                return S_OK;
            }
        }
    }

    Logger::Debug("JITCompilationStarted: Loader registered.");

    return S_OK;
}
#endif

WSTRING CorProfiler::GetBytecodeInstrumentationAssembly() const
{
    WSTRING bytecodeInstrumentationAssembly = managed_profiler_full_assembly_version;
    if (!runtime_information_.runtime_type)
    {
        Logger::Error("GetBytecodeInstrumentationAssembly: called before runtime_information was initialized.");
    }
    else if (runtime_information_.is_desktop())
    {
        // When on .NET Framework use the signature with the public key so strong name works.
        bytecodeInstrumentationAssembly = managed_profiler_full_assembly_version_strong_name;
    }

    return bytecodeInstrumentationAssembly;
}

//
// Helper methods
//
bool CorProfiler::GetIntegrationTypeRef(ModuleMetadata&              module_metadata,
                                        ModuleID                     module_id,
                                        const IntegrationDefinition& integration_definition,
                                        mdTypeRef&                   integration_type_ref)
{
    const auto& integration_key = integration_definition.integration_type.get_cache_key();

    if (!module_metadata.TryGetIntegrationTypeRef(integration_key, integration_type_ref))
    {
        const auto& module_info = GetModuleInfo(this->info_, module_id);
        if (!module_info.IsValid())
        {
            return false;
        }

        mdModule module;
        auto     hr = module_metadata.metadata_import->GetModuleFromScope(&module);
        if (FAILED(hr))
        {
            Logger::Warn("GetIntegrationTypeRef failed to get module metadata token for "
                         "module_id=",
                         module_id, " module_name=", module_info.assembly.name);
            return false;
        }

        const MetadataBuilder metadata_builder(module_metadata, module, module_metadata.metadata_import,
                                               module_metadata.metadata_emit, module_metadata.assembly_import,
                                               module_metadata.assembly_emit);

        // for each wrapper assembly, emit an assembly reference
        hr = metadata_builder.EmitAssemblyRef(integration_definition.integration_type.assembly);
        if (FAILED(hr))
        {
            Logger::Warn("GetIntegrationTypeRef failed to emit wrapper assembly ref for assembly=",
                         integration_definition.integration_type.assembly.name, ", Version=",
                         integration_definition.integration_type.assembly.version.str(), ", Culture=",
                         integration_definition.integration_type.assembly.locale, " PublicKeyToken=",
                         integration_definition.integration_type.assembly.public_key.str());
            return false;
        }

        // for each method replacement in each enabled integration,
        // emit a reference to the instrumentation wrapper type
        hr = metadata_builder.FindIntegrationTypeRef(integration_definition, integration_type_ref);
        if (FAILED(hr))
        {
            Logger::Warn("GetIntegrationTypeRef failed to obtain wrapper method ref for ",
                         integration_definition.integration_type.name, ".");
            return false;
        }
    }

    return true;
}

bool CorProfiler::ProfilerAssemblyIsLoadedIntoAppDomain(AppDomainID app_domain_id)
{
    return managed_profiler_loaded_domain_neutral ||
           managed_profiler_loaded_app_domains.find(app_domain_id) != managed_profiler_loaded_app_domains.end();
}

const std::string indent_values[] = {
    "",
    std::string(2 * 1, ' '),
    std::string(2 * 2, ' '),
    std::string(2 * 3, ' '),
    std::string(2 * 4, ' '),
    std::string(2 * 5, ' '),
    std::string(2 * 6, ' '),
    std::string(2 * 7, ' '),
    std::string(2 * 8, ' '),
    std::string(2 * 9, ' '),
    std::string(2 * 10, ' '),
};

std::string CorProfiler::GetILCodes(const std::string&              title,
                                    ILRewriter*                     rewriter,
                                    const FunctionInfo&             caller,
                                    const ComPtr<IMetaDataImport2>& metadata_import)
{
    std::stringstream orig_sstream;
    orig_sstream << title;
    orig_sstream << ToString(caller.type.name);
    orig_sstream << ".";
    orig_sstream << ToString(caller.name);
    orig_sstream << " => (max_stack: ";
    orig_sstream << rewriter->GetMaxStackValue();
    orig_sstream << ")" << std::endl;

    const auto& ehCount = rewriter->GetEHCount();
    const auto& ehPtr   = rewriter->GetEHPointer();
    int         indent  = 1;

    PCCOR_SIGNATURE originalSignature     = nullptr;
    ULONG           originalSignatureSize = 0;
    mdToken         localVarSig           = rewriter->GetTkLocalVarSig();

    if (localVarSig != mdTokenNil)
    {
        auto hr = metadata_import->GetSigFromToken(localVarSig, &originalSignature, &originalSignatureSize);
        if (SUCCEEDED(hr))
        {
            orig_sstream << std::endl
                         << ". Local Var Signature: " << ToString(HexStr(originalSignature, originalSignatureSize))
                         << std::endl;
        }
    }

    orig_sstream << std::endl;
    for (ILInstr* cInstr = rewriter->GetILList()->m_pNext; cInstr != rewriter->GetILList(); cInstr = cInstr->m_pNext)
    {

        if (ehCount > 0)
        {
            for (unsigned int i = 0; i < ehCount; i++)
            {
                const auto& currentEH = ehPtr[i];
                if (currentEH.m_Flags == COR_ILEXCEPTION_CLAUSE_FINALLY)
                {
                    if (currentEH.m_pTryBegin == cInstr)
                    {
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << ".try {" << std::endl;
                        indent++;
                    }
                    if (currentEH.m_pTryEnd == cInstr)
                    {
                        indent--;
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << "}" << std::endl;
                    }
                    if (currentEH.m_pHandlerBegin == cInstr)
                    {
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << ".finally {" << std::endl;
                        indent++;
                    }
                }
            }
            for (unsigned int i = 0; i < ehCount; i++)
            {
                const auto& currentEH = ehPtr[i];
                if (currentEH.m_Flags == COR_ILEXCEPTION_CLAUSE_NONE)
                {
                    if (currentEH.m_pTryBegin == cInstr)
                    {
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << ".try {" << std::endl;
                        indent++;
                    }
                    if (currentEH.m_pTryEnd == cInstr)
                    {
                        indent--;
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << "}" << std::endl;
                    }
                    if (currentEH.m_pHandlerBegin == cInstr)
                    {
                        if (indent > 0)
                        {
                            orig_sstream << indent_values[indent];
                        }
                        orig_sstream << ".catch {" << std::endl;
                        indent++;
                    }
                }
            }
        }

        if (indent > 0)
        {
            orig_sstream << indent_values[indent];
        }
        orig_sstream << cInstr;
        orig_sstream << ": ";
        if (cInstr->m_opcode < opcodes_names.size())
        {
            orig_sstream << std::setw(10) << opcodes_names[cInstr->m_opcode];
        }
        else
        {
            orig_sstream << "0x";
            orig_sstream << std::setfill('0') << std::setw(2) << std::hex << cInstr->m_opcode;
        }
        if (cInstr->m_pTarget != NULL)
        {
            orig_sstream << "  ";
            orig_sstream << cInstr->m_pTarget;

            if (cInstr->m_opcode == CEE_CALL || cInstr->m_opcode == CEE_CALLVIRT || cInstr->m_opcode == CEE_NEWOBJ)
            {
                const auto memberInfo = GetFunctionInfo(metadata_import, (mdMemberRef)cInstr->m_Arg32);
                orig_sstream << "  | ";
                orig_sstream << ToString(memberInfo.type.name);
                orig_sstream << ".";
                orig_sstream << ToString(memberInfo.name);
                if (memberInfo.signature.NumberOfArguments() > 0)
                {
                    orig_sstream << "(";
                    orig_sstream << memberInfo.signature.NumberOfArguments();
                    orig_sstream << " argument{s}";
                    orig_sstream << ")";
                }
                else
                {
                    orig_sstream << "()";
                }
            }
            else if (cInstr->m_opcode == CEE_CASTCLASS || cInstr->m_opcode == CEE_BOX ||
                     cInstr->m_opcode == CEE_UNBOX_ANY || cInstr->m_opcode == CEE_NEWARR ||
                     cInstr->m_opcode == CEE_INITOBJ)
            {
                const auto typeInfo = GetTypeInfo(metadata_import, (mdTypeRef)cInstr->m_Arg32);
                orig_sstream << "  | ";
                orig_sstream << ToString(typeInfo.name);
            }
            else if (cInstr->m_opcode == CEE_LDSTR)
            {
                WCHAR szString[1024];
                ULONG szStringLength;
                auto  hr = metadata_import->GetUserString((mdString)cInstr->m_Arg32, szString, 1024, &szStringLength);
                if (SUCCEEDED(hr))
                {
                    orig_sstream << "  | \"";
                    orig_sstream << ToString(WSTRING(szString, szStringLength));
                    orig_sstream << "\"";
                }
            }
        }
        else if (cInstr->m_Arg64 != 0)
        {
            orig_sstream << " ";
            orig_sstream << cInstr->m_Arg64;
        }
        orig_sstream << std::endl;

        if (ehCount > 0)
        {
            for (unsigned int i = 0; i < ehCount; i++)
            {
                const auto& currentEH = ehPtr[i];
                if (currentEH.m_pHandlerEnd == cInstr)
                {
                    indent--;
                    if (indent > 0)
                    {
                        orig_sstream << indent_values[indent];
                    }
                    orig_sstream << "}" << std::endl;
                }
            }
        }
    }
    return orig_sstream.str();
}

#ifdef _WIN32
//
// Loader methods. These are only used on the .NET Framework.
//
HRESULT CorProfiler::RunAutoInstrumentationLoader(const ComPtr<IMetaDataEmit2>& metadata_emit,
                                                  const ModuleID                module_id,
                                                  const mdToken                 function_token,
                                                  const FunctionInfo&           caller,
                                                  const ModuleMetadata&         module_metadata)
{
    mdMethodDef ret_method_token;
    auto        hr = GenerateLoaderMethod(module_id, &ret_method_token);

    if (FAILED(hr))
    {
        Logger::Warn("RunAutoInstrumentationLoader: Call to GenerateLoaderMethod failed for ", module_id);
        return hr;
    }

    ILRewriter rewriter(this->info_, nullptr, module_id, function_token);
    hr = rewriter.Import();

    if (FAILED(hr))
    {
        Logger::Warn("RunAutoInstrumentationLoader: Call to ILRewriter.Import() failed for ", module_id, " ",
                     function_token);
        return hr;
    }

    ILRewriterWrapper rewriter_wrapper(&rewriter);

    // Get first instruction and set the rewriter to that location
    ILInstr* pInstr = rewriter.GetILList()->m_pNext;
    rewriter_wrapper.SetILPosition(pInstr);
    rewriter_wrapper.CallMember(ret_method_token, false);
    hr = rewriter.Export();

    if (FAILED(hr))
    {
        Logger::Warn("RunAutoInstrumentationLoader: Call to ILRewriter.Export() failed for ModuleID=", module_id, " ",
                     function_token);
        return hr;
    }

    return S_OK;
}

HRESULT CorProfiler::GenerateLoaderMethod(const ModuleID module_id, mdMethodDef* ret_method_token)
{
    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                             metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    const auto& assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
    const auto& assembly_emit   = metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

    mdAssemblyRef corlib_ref;
    hr = GetCorLibAssemblyRef(assembly_emit, corAssemblyProperty, &corlib_ref);

    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: failed to define AssemblyRef to mscorlib");
        return hr;
    }

    // Define a TypeRef for System.Object
    mdTypeRef object_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Object"), &object_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName failed");
        return hr;
    }

    // Define a new TypeDef __DDVoidMethodType__ that extends System.Object
    mdTypeDef new_type_def;
    hr = metadata_emit->DefineTypeDef(WStr("__DDVoidMethodType__"), tdAbstract | tdSealed, object_type_ref, NULL,
                                      &new_type_def);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeDef failed");
        return hr;
    }

    // Define a new static method __DDVoidMethodCall__ on the new type that has a void return type and takes no
    // arguments
    BYTE initialize_signature[] = {
        IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
        0,                             // Number of parameters
        ELEMENT_TYPE_VOID,             // Return type
    };
    hr = metadata_emit->DefineMethod(new_type_def, WStr("__DDVoidMethodCall__"), mdStatic, initialize_signature,
                                     sizeof(initialize_signature), 0, 0, ret_method_token);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMethod failed");
        return hr;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Define IsAlreadyLoaded() method
    //

    //
    // Define a new static method IsAlreadyLoaded on the new type that has a bool return type and takes no arguments;
    //
    mdMethodDef alreadyLoadedMethodToken;
    BYTE        already_loaded_signature[] = {
        IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_BOOLEAN,
    };
    hr = metadata_emit->DefineMethod(new_type_def, WStr("IsAlreadyLoaded"), mdStatic | mdPrivate,
                                     already_loaded_signature, sizeof(already_loaded_signature), 0, 0,
                                     &alreadyLoadedMethodToken);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMethod IsAlreadyLoaded failed");
        return hr;
    }

    // Define a new static int field _isAssemblyLoaded on the new type.
    mdFieldDef isAssemblyLoadedFieldToken = mdFieldDefNil;
    BYTE       field_signature[]          = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
    hr = metadata_emit->DefineField(new_type_def, WStr("_isAssemblyLoaded"), fdStatic | fdPrivate, field_signature,
                                    sizeof(field_signature), 0, nullptr, 0, &isAssemblyLoadedFieldToken);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineField _isAssemblyLoaded failed");
        return hr;
    }

    // Get a TypeRef for System.Threading.Interlocked
    mdTypeRef interlocked_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Threading.Interlocked"), &interlocked_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName interlocked_type_ref failed");
        return hr;
    }

    // Create method signature for System.Threading.Interlocked::CompareExchange(int32&, int32, int32)
    COR_SIGNATURE interlocked_compare_exchange_signature[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT,
                                                              3,
                                                              ELEMENT_TYPE_I4,
                                                              ELEMENT_TYPE_BYREF,
                                                              ELEMENT_TYPE_I4,
                                                              ELEMENT_TYPE_I4,
                                                              ELEMENT_TYPE_I4};

    mdMemberRef interlocked_compare_member_ref;
    hr =
        metadata_emit->DefineMemberRef(interlocked_type_ref, WStr("CompareExchange"),
                                       interlocked_compare_exchange_signature,
                                       sizeof(interlocked_compare_exchange_signature), &interlocked_compare_member_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMemberRef CompareExchange failed");
        return hr;
    }

    /////////////////////////////////////////////
    // Add IL instructions into the IsAlreadyLoaded method
    //
    //  static int _isAssemblyLoaded = 0;
    //
    //  public static bool IsAlreadyLoaded() {
    //      return Interlocked.CompareExchange(ref _isAssemblyLoaded, 1, 0) == 1;
    //  }
    //
    ILRewriter rewriter_already_loaded(this->info_, nullptr, module_id, alreadyLoadedMethodToken);
    rewriter_already_loaded.InitializeTiny();

    ILInstr* pALFirstInstr = rewriter_already_loaded.GetILList()->m_pNext;
    ILInstr* pALNewInstr   = NULL;

    // ldsflda _isAssemblyLoaded : Load the address of the "_isAssemblyLoaded" static var
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_LDSFLDA;
    pALNewInstr->m_Arg32  = isAssemblyLoadedFieldToken;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // ldc.i4.1 : Load the constant 1 (int) to the stack
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_LDC_I4_1;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // ldc.i4.0 : Load the constant 0 (int) to the stack
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_LDC_I4_0;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // call int Interlocked.CompareExchange(ref int, int, int) method
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_CALL;
    pALNewInstr->m_Arg32  = interlocked_compare_member_ref;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // ldc.i4.1 : Load the constant 1 (int) to the stack
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_LDC_I4_1;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // ceq : Compare equality from two values from the stack
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_CEQ;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    // ret : Return the value of the comparison
    pALNewInstr           = rewriter_already_loaded.NewILInstr();
    pALNewInstr->m_opcode = CEE_RET;
    rewriter_already_loaded.InsertBefore(pALFirstInstr, pALNewInstr);

    hr = rewriter_already_loaded.Export();
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: Call to ILRewriter.Export() failed for ModuleID=", module_id);
        return hr;
    }

    // Define a method on the managed side that will PInvoke into the profiler method:
    // C++: void GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize, BYTE** pSymbolsArray, int*
    // symbolsSize) C#: static extern void GetAssemblyAndSymbolsBytes(out IntPtr assemblyPtr, out int assemblySize, out
    // IntPtr symbolsPtr, out int symbolsSize)
    mdMethodDef   pinvoke_method_def;
    COR_SIGNATURE get_assembly_bytes_signature[] = {
        IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
        4,                             // Number of parameters
        ELEMENT_TYPE_VOID,             // Return type
        ELEMENT_TYPE_BYREF,            // List of parameter types
        ELEMENT_TYPE_I,
        ELEMENT_TYPE_BYREF,
        ELEMENT_TYPE_I4,
        ELEMENT_TYPE_BYREF,
        ELEMENT_TYPE_I,
        ELEMENT_TYPE_BYREF,
        ELEMENT_TYPE_I4,
    };
    hr = metadata_emit->DefineMethod(new_type_def, WStr("GetAssemblyAndSymbolsBytes"),
                                     mdStatic | mdPinvokeImpl | mdHideBySig, get_assembly_bytes_signature,
                                     sizeof(get_assembly_bytes_signature), 0, 0, &pinvoke_method_def);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMethod failed");
        return hr;
    }

    metadata_emit->SetMethodImplFlags(pinvoke_method_def, miPreserveSig);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: SetMethodImplFlags failed");
        return hr;
    }

    WSTRING native_profiler_file = GetCurrentModuleFileName();
    Logger::Debug("GenerateVoidILStartupMethod: Setting the PInvoke native profiler library path to ",
                  native_profiler_file);

    mdModuleRef profiler_ref;
    hr = metadata_emit->DefineModuleRef(native_profiler_file.c_str(), &profiler_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineModuleRef failed");
        return hr;
    }

    hr = metadata_emit->DefinePinvokeMap(pinvoke_method_def, 0, WStr("GetAssemblyAndSymbolsBytes"), profiler_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefinePinvokeMap failed");
        return hr;
    }

    // Get a TypeRef for System.Byte
    mdTypeRef byte_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Byte"), &byte_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName failed");
        return hr;
    }

    // Get a TypeRef for System.Runtime.InteropServices.Marshal
    mdTypeRef marshal_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Runtime.InteropServices.Marshal"),
                                            &marshal_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName failed");
        return hr;
    }

    // Get a MemberRef for System.Runtime.InteropServices.Marshal.Copy(IntPtr, Byte[], int, int)
    mdMemberRef   marshal_copy_member_ref;
    COR_SIGNATURE marshal_copy_signature[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
                                              4,                             // Number of parameters
                                              ELEMENT_TYPE_VOID,             // Return type
                                              ELEMENT_TYPE_I,                // List of parameter types
                                              ELEMENT_TYPE_SZARRAY,
                                              ELEMENT_TYPE_U1,
                                              ELEMENT_TYPE_I4,
                                              ELEMENT_TYPE_I4};
    hr = metadata_emit->DefineMemberRef(marshal_type_ref, WStr("Copy"), marshal_copy_signature,
                                        sizeof(marshal_copy_signature), &marshal_copy_member_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMemberRef failed");
        return hr;
    }

    // Get a TypeRef for System.Reflection.Assembly
    mdTypeRef system_reflection_assembly_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Reflection.Assembly"),
                                            &system_reflection_assembly_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName failed");
        return hr;
    }

    // Get a MemberRef for System.Object.ToString()
    mdTypeRef system_object_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.Object"), &system_object_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName failed");
        return hr;
    }

    // Create method signature for System.Reflection.Assembly.Load(byte[], byte[])
    COR_SIGNATURE appdomain_load_signature_start[] = {
        IMAGE_CEE_CS_CALLCONV_DEFAULT, 2,
        ELEMENT_TYPE_CLASS // ret = System.Reflection.Assembly
        // insert compressed token for System.Reflection.Assembly TypeRef here
    };
    COR_SIGNATURE appdomain_load_signature_end[] = {ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_U1, ELEMENT_TYPE_SZARRAY,
                                                    ELEMENT_TYPE_U1};
    ULONG start_length = sizeof(appdomain_load_signature_start);
    ULONG end_length   = sizeof(appdomain_load_signature_end);

    BYTE  system_reflection_assembly_type_ref_compressed_token[4];
    ULONG token_length =
        CorSigCompressToken(system_reflection_assembly_type_ref, system_reflection_assembly_type_ref_compressed_token);

    const auto    appdomain_load_signature_length = start_length + token_length + end_length;
    COR_SIGNATURE appdomain_load_signature[250];
    memcpy(appdomain_load_signature, appdomain_load_signature_start, start_length);
    memcpy(&appdomain_load_signature[start_length], system_reflection_assembly_type_ref_compressed_token, token_length);
    memcpy(&appdomain_load_signature[start_length + token_length], appdomain_load_signature_end, end_length);

    mdMemberRef appdomain_load_member_ref;
    hr = metadata_emit->DefineMemberRef(system_reflection_assembly_type_ref, WStr("Load"), appdomain_load_signature,
                                        appdomain_load_signature_length, &appdomain_load_member_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMemberRef failed");
        return hr;
    }

    // Create method signature for Assembly.CreateInstance(string)
    COR_SIGNATURE assembly_create_instance_signature[] = {IMAGE_CEE_CS_CALLCONV_HASTHIS, 1,
                                                          ELEMENT_TYPE_OBJECT, // ret = System.Object
                                                          ELEMENT_TYPE_STRING};

    mdMemberRef assembly_create_instance_member_ref;
    hr = metadata_emit->DefineMemberRef(system_reflection_assembly_type_ref, WStr("CreateInstance"),
                                        assembly_create_instance_signature, sizeof(assembly_create_instance_signature),
                                        &assembly_create_instance_member_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineMemberRef failed");
        return hr;
    }

    // Create a string representing "OpenTelemetry.AutoInstrumentation.Loader.Loader"
    LPCWSTR load_helper_str      = L"OpenTelemetry.AutoInstrumentation.Loader.Loader";
    auto    load_helper_str_size = wcslen(load_helper_str);

    mdString load_helper_token;
    hr = metadata_emit->DefineUserString(load_helper_str, (ULONG)load_helper_str_size, &load_helper_token);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineUserString failed");
        return hr;
    }

    // Generate a locals signature defined in the following way:
    //   [0] System.IntPtr ("assemblyPtr" - address of assembly bytes)
    //   [1] System.Int32  ("assemblySize" - size of assembly bytes)
    //   [2] System.IntPtr ("symbolsPtr" - address of symbols bytes)
    //   [3] System.Int32  ("symbolsSize" - size of symbols bytes)
    //   [4] System.Byte[] ("assemblyBytes" - managed byte array for assembly)
    //   [5] System.Byte[] ("symbolsBytes" - managed byte array for symbols)
    //   [6] class System.Reflection.Assembly ("loadedAssembly" - assembly instance to save loaded assembly)
    mdSignature   locals_signature_token;
    COR_SIGNATURE locals_signature[15] = {
        IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, // Calling convention
        7,                               // Number of variables
        ELEMENT_TYPE_I,                  // List of variable types
        ELEMENT_TYPE_I4, ELEMENT_TYPE_I, ELEMENT_TYPE_I4, ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_U1, ELEMENT_TYPE_SZARRAY,
        ELEMENT_TYPE_U1, ELEMENT_TYPE_CLASS
        // insert compressed token for System.Reflection.Assembly TypeRef here
    };
    CorSigCompressToken(system_reflection_assembly_type_ref, &locals_signature[11]);
    hr = metadata_emit->GetTokenFromSig(locals_signature, sizeof(locals_signature), &locals_signature_token);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: Unable to generate locals signature. ModuleID=", module_id);
        return hr;
    }

    /////////////////////////////////////////////
    // Add IL instructions into the void method
    ILRewriter rewriter_void(this->info_, nullptr, module_id, *ret_method_token);
    rewriter_void.InitializeTiny();
    rewriter_void.SetTkLocalVarSig(locals_signature_token);

    ILInstr* pFirstInstr = rewriter_void.GetILList()->m_pNext;
    ILInstr* pNewInstr   = NULL;

    // Step 0) Check if the assembly was already loaded

    // call bool IsAlreadyLoaded()
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32  = alreadyLoadedMethodToken;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // check if the return of the method call is true or false
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_BRFALSE_S;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
    ILInstr* pBranchFalseInstr = pNewInstr;

    // return if IsAlreadyLoaded is true
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_RET;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Step 1) Call void GetAssemblyAndSymbolsBytes(out IntPtr assemblyPtr, out int assemblySize, out IntPtr symbolsPtr,
    // out int symbolsSize)

    // ldloca.s 0 : Load the address of the "assemblyPtr" variable (locals index 0)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOCA_S;
    pNewInstr->m_Arg32  = 0;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Set the false branch target
    pBranchFalseInstr->m_pTarget = pNewInstr;

    // ldloca.s 1 : Load the address of the "assemblySize" variable (locals index 1)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOCA_S;
    pNewInstr->m_Arg32  = 1;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloca.s 2 : Load the address of the "symbolsPtr" variable (locals index 2)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOCA_S;
    pNewInstr->m_Arg32  = 2;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloca.s 3 : Load the address of the "symbolsSize" variable (locals index 3)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOCA_S;
    pNewInstr->m_Arg32  = 3;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // call void GetAssemblyAndSymbolsBytes(out IntPtr assemblyPtr, out int assemblySize, out IntPtr symbolsPtr, out int
    // symbolsSize)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32  = pinvoke_method_def;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Step 2) Call void Marshal.Copy(IntPtr source, byte[] destination, int startIndex, int length) to populate the
    // managed assembly bytes

    // ldloc.1 : Load the "assemblySize" variable (locals index 1)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_1;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // newarr System.Byte : Create a new Byte[] to hold a managed copy of the assembly data
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_NEWARR;
    pNewInstr->m_Arg32  = byte_type_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // stloc.s 4 : Assign the Byte[] to the "assemblyBytes" variable (locals index 4)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_STLOC_S;
    pNewInstr->m_Arg8   = 4;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.0 : Load the "assemblyPtr" variable (locals index 0)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_0;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.s 4 : Load the "assemblyBytes" variable (locals index 4)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_S;
    pNewInstr->m_Arg8   = 4;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldc.i4.0 : Load the integer 0 for the Marshal.Copy startIndex parameter
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDC_I4_0;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.1 : Load the "assemblySize" variable (locals index 1) for the Marshal.Copy length parameter
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_1;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // call Marshal.Copy(IntPtr source, byte[] destination, int startIndex, int length)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32  = marshal_copy_member_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Step 3) Call void Marshal.Copy(IntPtr source, byte[] destination, int startIndex, int length) to populate the
    // symbols bytes

    // ldloc.3 : Load the "symbolsSize" variable (locals index 3)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_3;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // newarr System.Byte : Create a new Byte[] to hold a managed copy of the symbols data
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_NEWARR;
    pNewInstr->m_Arg32  = byte_type_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // stloc.s 5 : Assign the Byte[] to the "symbolsBytes" variable (locals index 5)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_STLOC_S;
    pNewInstr->m_Arg8   = 5;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.2 : Load the "symbolsPtr" variables (locals index 2)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_2;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.s 5 : Load the "symbolsBytes" variable (locals index 5)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_S;
    pNewInstr->m_Arg8   = 5;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldc.i4.0 : Load the integer 0 for the Marshal.Copy startIndex parameter
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDC_I4_0;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.3 : Load the "symbolsSize" variable (locals index 3) for the Marshal.Copy length parameter
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_3;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // call void Marshal.Copy(IntPtr source, byte[] destination, int startIndex, int length)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32  = marshal_copy_member_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Step 4) Call System.Reflection.Assembly System.Reflection.Assembly.Load(byte[], byte[]))

    // ldloc.s 4 : Load the "assemblyBytes" variable (locals index 4) for the first byte[] parameter of
    // AppDomain.Load(byte[], byte[])
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_S;
    pNewInstr->m_Arg8   = 4;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldloc.s 5 : Load the "symbolsBytes" variable (locals index 5) for the second byte[] parameter of
    // AppDomain.Load(byte[], byte[])
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_S;
    pNewInstr->m_Arg8   = 5;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // call System.Reflection.Assembly System.Reflection.Assembly.Load(uint8[], uint8[])
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALL;
    pNewInstr->m_Arg32  = appdomain_load_member_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // stloc.s 6 : Assign the System.Reflection.Assembly object to the "loadedAssembly" variable (locals index 6)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_STLOC_S;
    pNewInstr->m_Arg8   = 6;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // Step 4) Call instance method Assembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Loader")

    // ldloc.s 6 : Load the "loadedAssembly" variable (locals index 6) to call Assembly.CreateInstance
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDLOC_S;
    pNewInstr->m_Arg8   = 6;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // ldstr "OpenTelemetry.AutoInstrumentation.Loader.Loader"
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_LDSTR;
    pNewInstr->m_Arg32  = load_helper_token;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // callvirt System.Object System.Reflection.Assembly.CreateInstance(string)
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_CALLVIRT;
    pNewInstr->m_Arg32  = assembly_create_instance_member_ref;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // pop the returned object
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_POP;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    // return
    pNewInstr           = rewriter_void.NewILInstr();
    pNewInstr->m_opcode = CEE_RET;
    rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

    if (IsDumpILRewriteEnabled())
    {
        mdToken      token = 0;
        TypeInfo     typeInfo{};
        WSTRING      methodName = WStr("__DDVoidMethodCall__");
        FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
        Logger::Info(
            GetILCodes("*** GenerateLoaderMethod(): Modified Code: ", &rewriter_void, caller, metadata_import));
    }

    hr = rewriter_void.Export();
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: Call to ILRewriter.Export() failed for ModuleID=", module_id);
        return hr;
    }

    return S_OK;
}

HRESULT CorProfiler::AddIISPreStartInitFlags(const ModuleID module_id, const mdToken function_token)
{
    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = this->info_->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                             metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    const auto& assembly_import = metadata_interfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
    const auto& assembly_emit   = metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

    ILRewriter rewriter(this->info_, nullptr, module_id, function_token);
    hr = rewriter.Import();

    if (FAILED(hr))
    {
        Logger::Warn("RunAutoInstrumentationLoader: Call to ILRewriter.Import() failed for ", module_id, " ",
                     function_token);
        return hr;
    }

    ILRewriterWrapper rewriter_wrapper(&rewriter);

    // Get corlib assembly ref
    mdAssemblyRef corlib_ref;
    hr = GetCorLibAssemblyRef(assembly_emit, corAssemblyProperty, &corlib_ref);

    // Get System.Boolean type token
    mdToken boolToken;
    metadata_emit->DefineTypeRefByName(corlib_ref, SystemBoolean, &boolToken);

    // Get System.AppDomain type ref
    mdTypeRef system_appdomain_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("System.AppDomain"), &system_appdomain_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("Wrapper objectTypeRef could not be defined.");
        return hr;
    }

    // Get a MemberRef for System.AppDomain.get_CurrentDomain()
    COR_SIGNATURE appdomain_get_current_domain_signature_start[] = {
        IMAGE_CEE_CS_CALLCONV_DEFAULT, 0,
        ELEMENT_TYPE_CLASS, // ret = System.AppDomain
        // insert compressed token for System.AppDomain TypeRef here
    };
    ULONG start_length = sizeof(appdomain_get_current_domain_signature_start);

    BYTE  system_appdomain_type_ref_compressed_token[4];
    ULONG token_length = CorSigCompressToken(system_appdomain_type_ref, system_appdomain_type_ref_compressed_token);

    const auto    appdomain_get_current_domain_signature_length = start_length + token_length;
    COR_SIGNATURE appdomain_get_current_domain_signature[250];
    memcpy(appdomain_get_current_domain_signature, appdomain_get_current_domain_signature_start, start_length);
    memcpy(&appdomain_get_current_domain_signature[start_length], system_appdomain_type_ref_compressed_token,
           token_length);

    mdMemberRef appdomain_get_current_domain_member_ref;
    hr = metadata_emit->DefineMemberRef(system_appdomain_type_ref, WStr("get_CurrentDomain"),
                                        appdomain_get_current_domain_signature,
                                        appdomain_get_current_domain_signature_length,
                                        &appdomain_get_current_domain_member_ref);

    // Get AppDomain.SetData
    COR_SIGNATURE appdomain_set_data_signature[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT |
                                                        IMAGE_CEE_CS_CALLCONV_HASTHIS, // Calling convention
                                                    2,                                 // Number of parameters
                                                    ELEMENT_TYPE_VOID,                 // Return type
                                                    ELEMENT_TYPE_STRING,               // List of parameter types
                                                    ELEMENT_TYPE_OBJECT};
    mdMemberRef appdomain_set_data_member_ref;
    hr = metadata_emit->DefineMemberRef(system_appdomain_type_ref, WStr("SetData"), appdomain_set_data_signature,
                                        sizeof(appdomain_set_data_signature), &appdomain_set_data_member_ref);

    LPCWSTR pre_init_start_str      = L"OpenTelemetry_IISPreInitStart";
    auto    pre_init_start_str_size = wcslen(pre_init_start_str);

    mdString pre_init_start_string_token;
    hr = metadata_emit->DefineUserString(pre_init_start_str, (ULONG)pre_init_start_str_size,
                                         &pre_init_start_string_token);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineUserString failed");
        return hr;
    }

    // Get first instruction and set the rewriter to that location
    ILInstr* pInstr = rewriter.GetILList()->m_pNext;
    rewriter_wrapper.SetILPosition(pInstr);
    ILInstr* pCurrentInstr = NULL;
    ILInstr* pNewInstr     = NULL;

    //////////////////////////////////////////////////
    // At the beginning of the method, call
    // AppDomain.CurrentDomain.SetData(string, true)

    // Call AppDomain.get_CurrentDomain
    rewriter_wrapper.CallMember(appdomain_get_current_domain_member_ref, false);

    // ldstr "OpenTelemetry_IISPreInitStart"
    pCurrentInstr       = rewriter_wrapper.GetCurrentILInstr();
    pNewInstr           = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LDSTR;
    pNewInstr->m_Arg32  = pre_init_start_string_token;
    rewriter.InsertBefore(pCurrentInstr, pNewInstr);

    // load a boxed version of the boolean true
    rewriter_wrapper.LoadInt32(1);
    rewriter_wrapper.Box(boolToken);

    // Call AppDomain.SetData(string, object)
    rewriter_wrapper.CallMember(appdomain_set_data_member_ref, true);

    //////////////////////////////////////////////////
    // At the end of the method, call
    // AppDomain.CurrentDomain.SetData(string, false)
    pInstr = rewriter.GetILList()->m_pPrev; // The last instruction should be a 'ret' instruction

    // Append a ret instruction so we can use the existing ret as the first instruction for our rewriting
    pNewInstr           = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_RET;
    rewriter.InsertAfter(pInstr, pNewInstr);
    rewriter_wrapper.SetILPosition(pNewInstr);

    // Call AppDomain.get_CurrentDomain
    // Special case: rewrite the previous ret instruction with this call
    pInstr->m_opcode = CEE_CALL;
    pInstr->m_Arg32  = appdomain_get_current_domain_member_ref;

    // ldstr "OpenTelemetry_IISPreInitStart"
    pCurrentInstr       = rewriter_wrapper.GetCurrentILInstr();
    pNewInstr           = rewriter.NewILInstr();
    pNewInstr->m_opcode = CEE_LDSTR;
    pNewInstr->m_Arg32  = pre_init_start_string_token;
    rewriter.InsertBefore(pCurrentInstr, pNewInstr);

    // load a boxed version of the boolean false
    rewriter_wrapper.LoadInt32(0);
    rewriter_wrapper.Box(boolToken);

    // Call AppDomain.SetData(string, object)
    rewriter_wrapper.CallMember(appdomain_set_data_member_ref, true);

    //////////////////////////////////////////////////
    // Finished with the IL rewriting, save the result
    hr = rewriter.Export();

    if (FAILED(hr))
    {
        Logger::Warn("RunAutoInstrumentationLoader: Call to ILRewriter.Export() failed for ModuleID=", module_id, " ",
                     function_token);
        return hr;
    }

    return S_OK;
}

void CorProfiler::GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray,
                                             int*   assemblySize,
                                             BYTE** pSymbolsArray,
                                             int*   symbolsSize) const
{
    if (!runtime_information_.is_desktop())
    {
        // On .NET the StartupHook is in charge of injecting the main managed module.
        return;
    }

    HINSTANCE hInstance     = DllHandle;
    LPCWSTR   dllLpName     = MAKEINTRESOURCE(NETFRAMEWORK_MANAGED_ENTRYPOINT_DLL);
    LPCWSTR   symbolsLpName = MAKEINTRESOURCE(NETFRAMEWORK_MANAGED_ENTRYPOINT_SYMBOLS);

    HRSRC   hResAssemblyInfo = FindResource(hInstance, dllLpName, L"ASSEMBLY");
    HGLOBAL hResAssembly     = LoadResource(hInstance, hResAssemblyInfo);
    *assemblySize            = SizeofResource(hInstance, hResAssemblyInfo);
    *pAssemblyArray          = (LPBYTE)LockResource(hResAssembly);

    HRSRC   hResSymbolsInfo = FindResource(hInstance, symbolsLpName, L"SYMBOLS");
    HGLOBAL hResSymbols     = LoadResource(hInstance, hResSymbolsInfo);
    *symbolsSize            = SizeofResource(hInstance, hResSymbolsInfo);
    *pSymbolsArray          = (LPBYTE)LockResource(hResSymbols);
}
#endif

// ***
// * ReJIT Methods
// ***

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITCompilationStarted(FunctionID functionId,
                                                               ReJITID    rejitId,
                                                               BOOL       fIsSafeToBlock)
{
    if (!is_attached_)
    {
        return S_OK;
    }

    Logger::Debug("ReJITCompilationStarted: [functionId: ", functionId, ", rejitId: ", rejitId, ", safeToBlock: ",
                  fIsSafeToBlock, "]");

    // we notify the reJIT handler of this event
    return rejit_handler->NotifyReJITCompilationStarted(functionId, rejitId);
}

HRESULT STDMETHODCALLTYPE CorProfiler::GetReJITParameters(ModuleID                     moduleId,
                                                          mdMethodDef                  methodId,
                                                          ICorProfilerFunctionControl* pFunctionControl)
{
    if (!is_attached_)
    {
        return S_OK;
    }

    Logger::Debug("GetReJITParameters: [moduleId: ", moduleId, ", methodId: ", methodId, "]");

    // we notify the reJIT handler of this event and pass the module_metadata.
    return rejit_handler->NotifyReJITParameters(moduleId, methodId, pFunctionControl);
}

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITCompilationFinished(FunctionID functionId,
                                                                ReJITID    rejitId,
                                                                HRESULT    hrStatus,
                                                                BOOL       fIsSafeToBlock)
{
    if (is_attached_ && Logger::IsDebugEnabled())
    {
        Logger::Debug("ReJITCompilationFinished: [functionId: ", functionId, ", rejitId: ", rejitId, ", hrStatus: ",
                      HResultStr(hrStatus), ", safeToBlock: ", fIsSafeToBlock, "]");
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITError(ModuleID    moduleId,
                                                  mdMethodDef methodId,
                                                  FunctionID  functionId,
                                                  HRESULT     hrStatus)
{
    if (!is_attached_)
    {
        Logger::Warn("ReJITError: [functionId: ", functionId, ", moduleId: ", moduleId, ", methodId: ", methodId,
                     ", hrStatus: ", HResultStr(hrStatus), "]");
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction)
{
    auto _ = trace::Stats::Instance()->JITCachedFunctionSearchStartedMeasure();
    if (!is_attached_ || !pbUseCachedFunction)
    {
        return S_OK;
    }

    // keep this lock until we are done using the module,
    // to prevent it from unloading while in use
    std::lock_guard<std::mutex> guard(module_ids_lock_);

    // Extract Module metadata
    ModuleID module_id;
    mdToken  function_token = mdTokenNil;

    HRESULT hr = this->info_->GetFunctionInfo(functionId, nullptr, &module_id, &function_token);
    if (FAILED(hr))
    {
        Logger::Warn("JITCachedFunctionSearchStarted: Call to ICorProfilerInfo4.GetFunctionInfo() failed for ",
                     functionId);
        return S_OK;
    }

    // Call RequestRejit for register inliners and current NGEN module.
    if (rejit_handler != nullptr)
    {
        // Process the current module to detect inliners.
        rejit_handler->AddNGenInlinerModule(module_id);
    }

    // Verify that we have the metadata for this module
    if (!Contains(module_ids_, module_id))
    {
        // we haven't stored a ModuleMetadata for this module,
        // so there's nothing to do here, we accept the NGEN image.
        *pbUseCachedFunction = true;
        return S_OK;
    }

    const auto& module_info = GetModuleInfo(this->info_, module_id);
    const auto& appDomainId = module_info.assembly.app_domain_id;

    const bool has_loader_injected_in_appdomain =
        first_jit_compilation_app_domains.find(appDomainId) != first_jit_compilation_app_domains.end();

    if (!has_loader_injected_in_appdomain)
    {
        Logger::Debug("Disabling NGEN due to missing loader.");
        // The loader is missing in this AppDomain, we skip the NGEN image to allow the JITCompilationStart inject it.
        *pbUseCachedFunction = false;
        return S_OK;
    }

    *pbUseCachedFunction = true;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadCreated(ThreadID threadId)
{
    if (continuousProfiler != nullptr)
    {
        continuousProfiler->ThreadCreated(threadId);
    }
    return S_OK;
}
HRESULT STDMETHODCALLTYPE CorProfiler::ThreadDestroyed(ThreadID threadId)
{
    if (continuousProfiler != nullptr)
    {
        continuousProfiler->ThreadDestroyed(threadId);
    }
    return S_OK;
}
HRESULT STDMETHODCALLTYPE CorProfiler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (continuousProfiler != nullptr)
    {
        continuousProfiler->ThreadNameChanged(threadId, cchName, name);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::EventPipeEventDelivered(EVENTPIPE_PROVIDER provider,
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
                                                               UINT_PTR           stackFrames[])
{
    if (continuousProfiler != nullptr && eventId == 10 && eventVersion == 4)
    {
        continuousProfiler->AllocationTick(cbEventData, eventData);
    }
    return S_OK;
}

} // namespace trace
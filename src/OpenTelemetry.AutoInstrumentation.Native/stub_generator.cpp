// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stub_generator.h"

#include "cor_profiler.h"
#include "corhlpr.h"
#include "environment_variables.h"
#include "environment_variables_util.h"
#include "il_rewriter.h"
#include "il_rewriter_wrapper.h"
#include "logger.h"
#include "signature_builder.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"

namespace trace
{

StubGenerator::StubGenerator(CorProfiler*            profiler,
                             ICorProfilerInfo7*      pICorProfilerInfo,
                             const AssemblyProperty& corAssemblyProperty)
    : m_profiler(profiler)
    , m_pICorProfilerInfo(pICorProfilerInfo)
    , m_corAssemblyProperty(corAssemblyProperty)
{
}


StubGenerator::~StubGenerator() {}

#ifdef _WIN32

void StubGenerator::PatchAppDomainCreate(const ModuleID module_id)
{
    mdTypeDef     loader_type                   = mdTokenNil;
    mdMethodDef   init_method                   = mdTokenNil;
    mdMethodDef   patch_app_domain_setup_method = mdTokenNil;
    HRESULT hr = GenerateLoaderType(module_id, &loader_type, &init_method, &patch_app_domain_setup_method);

    if (FAILED(hr))
    {
        Logger::Error("Failed to inject loader type in mscorlib, module id ", module_id, ", result ", hr);
    }
    else
    {
        hr = ModifyAppDomainCreate(module_id, patch_app_domain_setup_method);

        if (FAILED(hr))
        {
            Logger::Warn("Failed to patch AppDomain creation, module id ", module_id, ", result ", hr);
        }
    }
}

// Add at the start of System.AppDomain::CreateDomain(string,System.Security.Policy.Evidence,System.AppDomainSetup)
// and System.AppDomainManager::CreateDomainHelper(string,System.Security.Policy.Evidence,System.AppDomainSetup)
// call to __DDVoidMethodType__::__DDPatchAppDomainSetup__ passing AppDomainSetup argument by ref there.
// If AppDomainSetup is null, it will be created. Resulting AppDomainSetup will be passed to
// OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater::ModifyConfig(System.AppDomainSetup appDomainSetup)
HRESULT StubGenerator::ModifyAppDomainCreate(const ModuleID module_id, mdMethodDef patch_app_domain_setup_method)
{
    // Expects to be called on mscorlib only
    // patch_app_domain_setup_method should be pre-injected in mscorlib
    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = m_pICorProfilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                         metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("ModifyAppDomainCreate: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);

    mdTypeDef system_app_domain_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.AppDomain"), mdTokenNil, &system_app_domain_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindTypeDefByName System.AppDomain failed");
            return hr;
        }
    }

    mdTypeDef system_app_domain_manager_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.AppDomainManager"), mdTokenNil,
                                                &system_app_domain_manager_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindTypeDefByName System.AppDomainManager failed");
            return hr;
        }
    }

    mdTypeDef system_security_policy_evidence_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.Security.Policy.Evidence"), mdTokenNil,
                                                &system_security_policy_evidence_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindTypeDefByName System.Security.Policy.Evidence failed");
            return hr;
        }
    }

    mdTypeDef system_app_domain_setup_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.AppDomainSetup"), mdTokenNil,
                                                &system_app_domain_setup_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindTypeDefByName System.AppDomainSetup failed");
            return hr;
        }
    }

    mdMethodDef system_app_domain_create_domain_token;
    {
        SignatureBuilder::StaticMethod
            system_app_domain_create_domain_signature{SignatureBuilder::Class(system_app_domain_token),
                                                      {SignatureBuilder::BuiltIn::String,
                                                       SignatureBuilder::Class(system_security_policy_evidence_token),
                                                       SignatureBuilder::Class(system_app_domain_setup_token)}};

        hr = metadata_import->FindMethod(system_app_domain_token, WStr("CreateDomain"),
                                         system_app_domain_create_domain_signature.Head(),
                                         system_app_domain_create_domain_signature.Size(),
                                         &system_app_domain_create_domain_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindMethod System.AppDomain::CreateDomain failed");
            return hr;
        }

        ILRewriter rewriter(m_pICorProfilerInfo, nullptr, module_id, system_app_domain_create_domain_token);
        hr = rewriter.Import();

        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: ILRewriter.Import System.AppDomain::CreateDomain failed");
            return hr;
        }

        ILInstr* pFirstInstr = rewriter.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARGA_S;
        pNewInstr->m_Arg8   = 2;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = patch_app_domain_setup_method;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        hr = rewriter.Export();

        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: ILRewriter.Export System.AppDomain::CreateDomain failed");
            return hr;
        }

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr("CreateDomain");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(m_profiler->GetILCodes("*** ModifyAppDomainCreate: Modified Code: ", &rewriter, caller, metadata_import));
        }
    }

    mdMethodDef system_app_domain_manager_create_domain_helper_token;
    {
        SignatureBuilder::StaticMethod system_app_domain_manager_create_domain_helper_signature{
            SignatureBuilder::Class(system_app_domain_token),
            {SignatureBuilder::BuiltIn::String, SignatureBuilder::Class(system_security_policy_evidence_token),
             SignatureBuilder::Class(system_app_domain_setup_token)}};

        hr = metadata_import->FindMethod(system_app_domain_manager_token, WStr("CreateDomainHelper"),
                                         system_app_domain_manager_create_domain_helper_signature.Head(),
                                         system_app_domain_manager_create_domain_helper_signature.Size(),
                                         &system_app_domain_manager_create_domain_helper_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: FindMethod System.AppDomainManager::CreateDomainHelper failed");
            return hr;
        }

        ILRewriter rewriter(m_pICorProfilerInfo, nullptr, module_id,
                            system_app_domain_manager_create_domain_helper_token);
        hr = rewriter.Import();

        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: ILRewriter.Import System.AppDomainManager::CreateDomainHelper failed");
            return hr;
        }

        ILInstr* pFirstInstr = rewriter.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARGA_S;
        pNewInstr->m_Arg8   = 2;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = patch_app_domain_setup_method;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        hr = rewriter.Export();

        if (FAILED(hr))
        {
            Logger::Warn("ModifyAppDomainCreate: ILRewriter.Export System.AppDomainManager::CreateDomainHelper failed");
            return hr;
        }

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr("CreateDomainHelper");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(m_profiler->GetILCodes("*** ModifyAppDomainCreate: Modified Code: ", &rewriter, caller, metadata_import));
        }
    }

    return S_OK;
}

// clang-format off
// This method will generate new type __DDVoidMethodType__ in target module.
// C# code for created class:
// public static class __DDVoidMethodType__
// {
//     private static System.Reflection.Assembly _assembly;
//     private static System.Action<System.AppDomainSetup> _assemblyFixer;
//     private static int _isAssemblyLoaded;
// 
//     static __DDVoidMethodType__()
//     {
//         GetAssemblyAndSymbolsBytes(out nint assemblyPtr, out int assemblySize, out nint symbolsPtr,
//             out int symbolsSize);
//         System.Byte[] assemblyBytes = new byte[assemblySize];
//         System.Runtime.InteropServices.Marshal.Copy(assemblyPtr, assemblyBytes, 0, assemblySize);
//         System.Byte[] symbolsBytes = new byte[symbolsSize];
//         System.Runtime.InteropServices.Marshal.Copy(symbolsPtr, symbolsBytes, 0, symbolsSize);
//         _assembly = System.Reflection.Assembly.Load(assemblyBytes, symbolsBytes);
//         _assemblyFixer = (System.Action<System.AppDomainSetup>)_assembly
//             .GetType("OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater")
//             .GetMethod("ModifyConfig")
//             .CreateDelegate(typeof(System.Action<System.AppDomainSetup>));
//     }
// 
//     public static void __DDVoidMethodCall__()
//     {
//         if (IsAlreadyLoaded())
//         {
//             return;
//         }
// 
//         _assembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Loader");
//     }
// 
//     public static void __DDPatchAppDomainSetup__(ref System.AppDomainSetup setup)
//     {
//         if (setup is null)
//         {
//             setup = new AppDomainSetup();
//         }
// 
//         _assemblyFixer(setup);
//     }
// 
//     private static bool IsAlreadyLoaded()
//     {
//         return System.Threading.Interlocked.CompareExchange(ref _isAssemblyLoaded, 1, 0) == 1;
//     }
// 
//     [System.Runtime.InteropServices.DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", PreserveSig = true)]
//     private static extern void GetAssemblyAndSymbolsBytes(out nint assemblyPtr, out int assemblySize, out nint symbolsPtr, out int symbolsSize);
// }
// clang-format on
HRESULT StubGenerator::GenerateLoaderType(const ModuleID module_id,
                                          mdTypeDef*     loader_type,
                                          mdMethodDef*   init_method,
                                          mdMethodDef*   patch_app_domain_setup_method)
{

    const auto& module_info = GetModuleInfo(this->m_pICorProfilerInfo, module_id);
    if (!module_info.IsValid())
    {
        Logger::Warn("GenerateLoaderType: failed to get module info ", module_id);

        return E_FAIL;
    }

    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = this->m_pICorProfilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite,
                                                                       IID_IMetaDataImport2,
                                                                       metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderType: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    const auto& assembly_emit   = metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

    MemberResolver resolver(metadata_import, metadata_emit);

    mdAssemblyRef corlib_ref = mdTokenNil;
    // We need assemblyRef only when we generate type outside of mscorlib
    // resolver will handle if we need to use typeRef/methodRef for other mscorlib-defined types
    // (if corlib_ref != mdTokenNil), or typeDef/methodDef can be used otherwise
    if (module_info.assembly.name != mscorlib_assemblyName)
    {
        mdAssemblyRef corlib_ref;
        hr = GetCorLibAssemblyRef(assembly_emit, m_corAssemblyProperty, &corlib_ref);

        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: failed to define AssemblyRef to mscorlib");
            return hr;
        }
    }

    // TypeDef/TypeRef for System.Object
    mdToken system_object_token;
    {
        hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Object"), &system_object_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Object failed");
            return hr;
        }
    }

    // .class public abstract auto ansi sealed __DDVoidMethodType__
    //        extends[mscorlib] System.Object
    {
        hr = metadata_emit->DefineTypeDef(WStr("__DDVoidMethodType__"), tdAbstract | tdSealed | tdPublic,
                                          system_object_token, NULL, loader_type);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineTypeDef __DDVoidMethodType__ failed");
            return hr;
        }
    }

    // TypeDef/TypeRef for System.Reflection.Assembly
    mdToken system_reflection_assembly_token;
    {
        hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Reflection.Assembly"),
                                            &system_reflection_assembly_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Reflection.Assembly failed");
            return hr;
        }
    }

    // .field private static class [mscorlib]System.Reflection.Assembly _assembly
    mdFieldDef assembly_field_token = mdFieldDefNil;
    {
        SignatureBuilder::Field assembly_field_signature{SignatureBuilder::Class{system_reflection_assembly_token}};
        hr = metadata_emit->DefineField(*loader_type, WStr("_assembly"), fdStatic | fdPrivate,
                                        assembly_field_signature.Head(), assembly_field_signature.Size(), 0, nullptr, 0,
                                        &assembly_field_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: Failed to define _assembly field");
            return hr;
        }
    }

    // TypeDef/TypeRef for System.Action`1
    mdToken system_action_token;
    {
        hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Action`1"), &system_action_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Action`1 failed");
            return hr;
        }
    }

    // TypeDef/TypeRef for System.AppDomainSetup
    mdToken system_app_domain_setup_token;
    {
        hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.AppDomainSetup"), &system_app_domain_setup_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.AppDomainSetup failed");
            return hr;
        }
    }

    // .field private static class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup> _assemblyFixer
    mdFieldDef app_domain_setup_fixer_field_token = mdFieldDefNil;
    {
        SignatureBuilder::Field app_domain_setup_fixer_signature{
            SignatureBuilder::GenericInstance{SignatureBuilder::Class{system_action_token},
                                              {SignatureBuilder::Class{system_app_domain_setup_token}}}};

        hr =
            metadata_emit->DefineField(*loader_type, WStr("_assemblyFixer"), fdStatic | fdPrivate,
                                       app_domain_setup_fixer_signature.Head(), app_domain_setup_fixer_signature.Size(),
                                       0, nullptr, 0, &app_domain_setup_fixer_field_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: Failed to define _assemblyFixer field");
            return hr;
        }
    }

    // .field private static int32 _isAssemblyLoaded
    mdFieldDef isAssemblyLoadedFieldToken = mdFieldDefNil;
    {
        BYTE field_signature[] = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
        hr = metadata_emit->DefineField(*loader_type, WStr("_isAssemblyLoaded"), fdStatic | fdPrivate, field_signature,
                                        sizeof(field_signature), 0, nullptr, 0, &isAssemblyLoadedFieldToken);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineField _isAssemblyLoaded failed");
            return hr;
        }
    }

    // .method private hidebysig specialname rtspecialname static
    //         void .cctor() cil managed
    mdMethodDef cctor_token;
    {
        BYTE cctor_signature[] = {
            IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
            0,                             // Number of parameters
            ELEMENT_TYPE_VOID,             // Return type
        };
        hr = metadata_emit->DefineMethod(*loader_type, WStr(".cctor"),
                                         mdPrivate | mdHideBySig | mdSpecialName | mdRTSpecialName | mdStatic,
                                         cctor_signature, sizeof(cctor_signature), 0, 0, &cctor_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineMethod .cctor failed");
            return hr;
        }
    }

    // .method private hidebysig static bool IsAlreadyLoaded() cil managed
    mdMethodDef already_loaded_method_token;
    {
        BYTE already_loaded_signature[] = {
            IMAGE_CEE_CS_CALLCONV_DEFAULT,
            0,
            ELEMENT_TYPE_BOOLEAN,
        };
        hr = metadata_emit->DefineMethod(*loader_type, WStr("IsAlreadyLoaded"), mdPrivate | mdHideBySig | mdStatic,
                                         already_loaded_signature, sizeof(already_loaded_signature), 0, 0,
                                         &already_loaded_method_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineMethod IsAlreadyLoaded failed");
            return hr;
        }
    }

    // .method public hidebysig static void  __DDVoidMethodCall__() cil managed
    {
        BYTE initialize_signature[] = {
            IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
            0,                             // Number of parameters
            ELEMENT_TYPE_VOID,             // Return type
        };
        hr = metadata_emit->DefineMethod(*loader_type, WStr("__DDVoidMethodCall__"), mdPublic | mdHideBySig | mdStatic,
                                         initialize_signature, sizeof(initialize_signature), 0, 0, init_method);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineMethod __DDVoidMethodCall__ failed");
            return hr;
        }
    }

    // .method public hidebysig static void  __DDPatchAppDomainSetup__(class [mscorlib]System.AppDomainSetup& setup) cil
    // managed
    {
        SignatureBuilder::StaticMethod
            app_domain_setup_fixer_method_signature{SignatureBuilder::BuiltIn::Void,
                                                    {SignatureBuilder::ByRef{
                                                        SignatureBuilder::Class{system_app_domain_setup_token}}}};

        hr = metadata_emit->DefineMethod(*loader_type, WStr("__DDPatchAppDomainSetup__"),
                                         mdPublic | mdHideBySig | mdStatic,
                                         app_domain_setup_fixer_method_signature.Head(),
                                         app_domain_setup_fixer_method_signature.Size(), 0, 0,
                                         patch_app_domain_setup_method);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineMethod __DDPatchAppDomainSetup__ failed");
            return hr;
        }
    }

    // TypeSpec for System.Action<System.AppDomainSetup>
    mdTypeSpec system_action_of_system_app_domain_setup_token;
    {
        SignatureBuilder::GenericInstance
            system_action_of_system_app_domain_setup_signature{SignatureBuilder::Class{system_action_token},
                                                               {SignatureBuilder::Class{
                                                                   system_app_domain_setup_token}}};
        hr = metadata_emit->GetTokenFromTypeSpec(system_action_of_system_app_domain_setup_signature.Head(),
                                                 system_action_of_system_app_domain_setup_signature.Size(),
                                                 &system_action_of_system_app_domain_setup_token);

        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: GetTokenFromTypeSpec System.Action`1<class "
                         "[mscorlib]System.AppDomainSetup> failed");
            return hr;
        }
    }

    // Define a method on the managed side that will PInvoke into the profiler method:
    // C++: void GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize, BYTE** pSymbolsArray, int*
    // symbolsSize)
    // C#: static extern void GetAssemblyAndSymbolsBytes(out IntPtr assemblyPtr, out int assemblySize, out
    // IntPtr symbolsPtr, out int symbolsSize)
    //
    // .method private hidebysig static pinvokeimpl("OpenTelemetry.AutoInstrumentation.Native.dll" winapi)
    //    void GetAssemblyAndSymbolsBytes([out] native int& assemblyPtr,
    //                                    [out] int32& assemblySize,
    //                                    [out] native int& symbolsPtr,
    //                                    [out] int32& symbolsSize)
    //    cil managed preservesig
    mdMethodDef pinvoke_method_def;
    {
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
        hr = metadata_emit->DefineMethod(*loader_type, WStr("GetAssemblyAndSymbolsBytes"),
                                         mdPrivate | mdHideBySig | mdStatic | mdPinvokeImpl,
                                         get_assembly_bytes_signature, sizeof(get_assembly_bytes_signature), 0, 0,
                                         &pinvoke_method_def);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineMethod GetAssemblyAndSymbolsBytes failed");
            return hr;
        }

        metadata_emit->SetMethodImplFlags(pinvoke_method_def, miPreserveSig);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: SetMethodImplFlags failed");
            return hr;
        }

        WSTRING native_profiler_file = GetCurrentModuleFileName();
        Logger::Debug("GenerateLoaderType: Setting the PInvoke native profiler library path to ", native_profiler_file);

        mdModuleRef profiler_ref;
        hr = metadata_emit->DefineModuleRef(native_profiler_file.c_str(), &profiler_ref);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefineModuleRef failed");
            return hr;
        }

        hr = metadata_emit->DefinePinvokeMap(pinvoke_method_def, 0, WStr("GetAssemblyAndSymbolsBytes"), profiler_ref);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: DefinePinvokeMap failed");
            return hr;
        }
    }

    // Add IL instructions into .cctor
    {
        // TypeRef/TypeDef for System.Byte
        mdToken byte_type_token;
        {
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Byte"), &byte_type_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Byte failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // void [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, uint8[], int32, int32)
        mdToken marshal_copy_token;
        {
            // TypeRef/TypeDef for [mscorlib]System.Runtime.InteropServices.Marshal
            mdTypeRef marshal_type_token;
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Runtime.InteropServices.Marshal"),
                                                &marshal_type_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: DefineTypeRefByName failed");
                return hr;
            }

            COR_SIGNATURE marshal_copy_signature[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
                                                      4,                             // Number of parameters
                                                      ELEMENT_TYPE_VOID,             // Return type
                                                      ELEMENT_TYPE_I,                // List of parameter types
                                                      ELEMENT_TYPE_SZARRAY,
                                                      ELEMENT_TYPE_U1,
                                                      ELEMENT_TYPE_I4,
                                                      ELEMENT_TYPE_I4};
            hr = resolver.GetMemberRefOrDef(marshal_type_token, WStr("Copy"), marshal_copy_signature,
                                            sizeof(marshal_copy_signature), &marshal_copy_token);
            if (FAILED(hr))
            {
                Logger::Warn(
                    "GenerateLoaderType: GetMemberRefOrDef System.Runtime.InteropServices.Marshal::Copy failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::Load(uint8[], uint8[])
        mdToken system_reflection_assembly_load_token;
        {
            SignatureBuilder::StaticMethod
                system_reflection_assembly_load_signature{SignatureBuilder::Class{system_reflection_assembly_token},
                                                          {SignatureBuilder::Array{SignatureBuilder::BuiltIn::Byte},
                                                           SignatureBuilder::Array{SignatureBuilder::BuiltIn::Byte}}};

            hr = resolver.GetMemberRefOrDef(system_reflection_assembly_token, WStr("Load"),
                                            system_reflection_assembly_load_signature.Head(),
                                            system_reflection_assembly_load_signature.Size(),
                                            &system_reflection_assembly_load_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.Reflection.Assembly::Load failed");
                return hr;
            }
        }

        // TypeRef/TypeDef for System.Type
        mdToken system_type_token;
        {
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Type"), &system_type_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Type failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // instance class [mscorlib]System.Type [mscorlib]System.Reflection.Assembly::GetType(string)
        mdToken system_reflection_assembly_get_type_token;
        {
            SignatureBuilder::InstanceMethod
                system_reflection_assembly_get_type_signature{SignatureBuilder::Class{system_type_token},
                                                              {SignatureBuilder::BuiltIn::String}};

            hr = resolver.GetMemberRefOrDef(system_reflection_assembly_token, WStr("GetType"),
                                            system_reflection_assembly_get_type_signature.Head(),
                                            system_reflection_assembly_get_type_signature.Size(),
                                            &system_reflection_assembly_get_type_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.Reflection.Assembly::GetType failed");
                return hr;
            }
        }

        // TypeRef/TypeDef for System.Reflection.MethodInfo
        mdToken system_reflection_method_info_token;
        {
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Reflection.MethodInfo"),
                                                &system_reflection_method_info_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Reflection.MethodInfo failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
        mdToken system_type_get_method_token;
        {
            SignatureBuilder::InstanceMethod system_type_get_method_signature =
                {SignatureBuilder::Class{system_reflection_method_info_token}, {SignatureBuilder::BuiltIn::String}};

            hr = resolver.GetMemberRefOrDef(system_type_token, WStr("GetMethod"),
                                            system_type_get_method_signature.Head(),
                                            system_type_get_method_signature.Size(), &system_type_get_method_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.Type::GetMethod(string) failed");
                return hr;
            }
        }

        // TypeRef/TypeDef for System.RuntimeTypeHandle
        mdToken system_runtime_type_handle_token;
        {
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.RuntimeTypeHandle"),
                                                &system_runtime_type_handle_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.RuntimeTypeHandle failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
        //   valuetype [mscorlib]System.RuntimeTypeHandle)
        mdMemberRef system_type_get_type_from_handle_token;
        {
            SignatureBuilder::StaticMethod
                system_type_get_type_from_handle_signature{SignatureBuilder::Class{system_type_token},
                                                           {SignatureBuilder::ValueType{
                                                               system_runtime_type_handle_token}}};

            hr = resolver.GetMemberRefOrDef(system_type_token, WStr("GetTypeFromHandle"),
                                            system_type_get_type_from_handle_signature.Head(),
                                            system_type_get_type_from_handle_signature.Size(),
                                            &system_type_get_type_from_handle_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.Type::GetTypeFromHandle failed");
                return hr;
            }
        }

        // TypeRef/TypeDef for System.Delegate
        mdTypeRef system_delegate_token;
        {
            hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Delegate"), &system_delegate_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName System.Delegate failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // instance class [mscorlib]System.Delegate [mscorlib]System.Reflection.MethodInfo::CreateDelegate(
        //   class [mscorlib]System.Type)
        mdMemberRef system_reflection_method_info_create_delegate_token;
        {
            SignatureBuilder::InstanceMethod
                system_reflection_method_info_create_delegate_signature{SignatureBuilder::Class{system_delegate_token},
                                                                        {SignatureBuilder::Class{system_type_token}}};

            hr = resolver.GetMemberRefOrDef(system_reflection_method_info_token, WStr("CreateDelegate"),
                                            system_reflection_method_info_create_delegate_signature.Head(),
                                            system_reflection_method_info_create_delegate_signature.Size(),
                                            &system_reflection_method_info_create_delegate_token);
            if (FAILED(hr))
            {
                Logger::Warn(
                    "GenerateLoaderType: GetMemberRefOrDef System.Reflection.MethodInfo::CreateDelegate failed");
                return hr;
            }
        }

        // Create a string representing "OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater"
        mdString config_updater_class_name_token;
        {
            LPCWSTR config_updater_class_name_str      = L"OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater";
            auto    config_updater_class_name_str_size = wcslen(config_updater_class_name_str);

            hr = metadata_emit->DefineUserString(config_updater_class_name_str,
                                                 (ULONG)config_updater_class_name_str_size,
                                                 &config_updater_class_name_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: DefineUserString "
                             "OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater failed");
                return hr;
            }
        }

        // Create a string representing "ModifyConfig"
        mdString config_updater_method_name_token;
        {
            LPCWSTR config_updater_method_name_str      = L"ModifyConfig";
            auto    config_updater_method_name_str_size = wcslen(config_updater_method_name_str);

            hr = metadata_emit->DefineUserString(config_updater_method_name_str,
                                                 (ULONG)config_updater_method_name_str_size,
                                                 &config_updater_method_name_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: DefineUserString ModifyConfig failed");
                return hr;
            }
        }

        // Add IL instructions into .cctor
        // clang-format off
        // .locals init (
        //   [0] native int assemblyPtr,
        //   [1] int32 assemblySize,
        //   [2] native int symbolsPtr,
        //   [3] int32 symbolsSize,
        //   [4] unsigned int8[] assemblyBytes,
        //   [5] unsigned int8[] symbolsBytes
        // )
        // 
        // IL_0000: ldloca.s     assemblyPtr
        // IL_0002: ldloca.s     assemblySize
        // IL_0004: ldloca.s     symbolsPtr
        // IL_0006: ldloca.s     symbolsSize
        // IL_0008: call         void __DDVoidMethodType__::GetAssemblyAndSymbolsBytes(native int&, int32&, native int&, int32&)
        // 
        // IL_000d: ldloc.1      // assemblySize
        // IL_000e: newarr       [mscorlib]System.Byte
        // IL_0013: stloc.s      assemblyBytes
        // 
        // IL_0015: ldloc.0      // assemblyPtr
        // IL_0016: ldloc.s      assemblyBytes
        // IL_0018: ldc.i4.0
        // IL_0019: ldloc.1      // assemblySize
        // IL_001a: call         void [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, unsigned int8[], int32, int32)
        // 
        // IL_001f: ldloc.3      // symbolsSize
        // IL_0020: newarr       [mscorlib]System.Byte
        // IL_0025: stloc.s      symbolsBytes
        // 
        // IL_0027: ldloc.2      // symbolsPtr
        // IL_0028: ldloc.s      symbolsBytes
        // IL_002a: ldc.i4.0
        // IL_002b: ldloc.3      // symbolsSize
        // IL_002c: call         void [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, unsigned int8[], int32, int32)
        // 
        // IL_0031: ldloc.s      assemblyBytes
        // IL_0033: ldloc.s      symbolsBytes
        // IL_0035: call         class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::Load(unsigned int8[], unsigned int8[])
        // IL_003a: stsfld       class [mscorlib]System.Reflection.Assembly __DDVoidMethodType__::_assembly
        // 
        // IL_003f: ldsfld       class [mscorlib]System.Reflection.Assembly __DDVoidMethodType__::_assembly
        // IL_0044: ldstr        "OpenTelemetry.AutoInstrumentation.Loader.AppConfigUpdater"
        // IL_0049: callvirt     instance class [mscorlib]System.Type [mscorlib]System.Reflection.Assembly::GetType(string)
        // IL_004e: ldstr        "ModifyConfig"
        // IL_0053: callvirt     instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
        // IL_0058: ldtoken      class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup>
        // IL_005d: call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        // IL_0062: callvirt     instance class [mscorlib]System.Delegate [mscorlib]System.Reflection.MethodInfo::CreateDelegate(class [mscorlib]System.Type)
        // IL_0067: castclass    class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup>
        // IL_006c: stsfld       class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup> __DDVoidMethodType__::_assemblyFixer
        // 
        // IL_0071: ret
        // clang-format on
        ILRewriter rewriter_void(m_pICorProfilerInfo, nullptr, module_id, cctor_token);
        rewriter_void.InitializeTiny();
        mdSignature locals_signature_token;
        {
            COR_SIGNATURE locals_signature[] = {IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, // Calling convention
                                                6,                               // Number of variables
                                                ELEMENT_TYPE_I,                  // List of variable types
                                                ELEMENT_TYPE_I4,
                                                ELEMENT_TYPE_I,
                                                ELEMENT_TYPE_I4,
                                                ELEMENT_TYPE_SZARRAY,
                                                ELEMENT_TYPE_U1,
                                                ELEMENT_TYPE_SZARRAY,
                                                ELEMENT_TYPE_U1};
            hr = metadata_emit->GetTokenFromSig(locals_signature, sizeof(locals_signature), &locals_signature_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: Unable to generate locals signature. ModuleID=", module_id);
                return hr;
            }
        }

        rewriter_void.SetTkLocalVarSig(locals_signature_token);

        ILInstr* pFirstInstr = rewriter_void.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOCA_S;
        pNewInstr->m_Arg32  = 0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOCA_S;
        pNewInstr->m_Arg32  = 1;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOCA_S;
        pNewInstr->m_Arg32  = 2;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOCA_S;
        pNewInstr->m_Arg32  = 3;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = pinvoke_method_def;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_1;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_NEWARR;
        pNewInstr->m_Arg32  = byte_type_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_STLOC_S;
        pNewInstr->m_Arg8   = 4;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_S;
        pNewInstr->m_Arg8   = 4;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDC_I4_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_1;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = marshal_copy_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_3;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_NEWARR;
        pNewInstr->m_Arg32  = byte_type_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_STLOC_S;
        pNewInstr->m_Arg8   = 5;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_2;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_S;
        pNewInstr->m_Arg8   = 5;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDC_I4_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_3;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = marshal_copy_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_S;
        pNewInstr->m_Arg8   = 4;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDLOC_S;
        pNewInstr->m_Arg8   = 5;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = system_reflection_assembly_load_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_STSFLD;
        pNewInstr->m_Arg32  = assembly_field_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSFLD;
        pNewInstr->m_Arg32  = assembly_field_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = config_updater_class_name_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALLVIRT;
        pNewInstr->m_Arg32  = system_reflection_assembly_get_type_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = config_updater_method_name_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALLVIRT;
        pNewInstr->m_Arg32  = system_type_get_method_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDTOKEN;
        pNewInstr->m_Arg32  = system_action_of_system_app_domain_setup_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = system_type_get_type_from_handle_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALLVIRT;
        pNewInstr->m_Arg32  = system_reflection_method_info_create_delegate_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CASTCLASS;
        pNewInstr->m_Arg32  = system_action_of_system_app_domain_setup_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_STSFLD;
        pNewInstr->m_Arg32  = app_domain_setup_fixer_field_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_RET;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr(".cctor");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(
                m_profiler->GetILCodes("*** GenerateLoaderType: Modified Code: ",
                                       &rewriter_void, caller, metadata_import));
        }

        hr = rewriter_void.Export();
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: Call to ILRewriter.Export() failed for ModuleID=", module_id);
            return hr;
        }
    }

    // Add IL instructions into the IsAlreadyLoaded method
    {
        // MethodRef/MethodDef for
        // int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&, int32, int32)
        mdMemberRef interlocked_compare_member_ref;
        {
            // TypeRef/TypeDef for System.Threading.Interlocked
            mdTypeRef interlocked_type_ref;
            {
                hr = resolver.GetTypeRefOrDefByName(corlib_ref, WStr("System.Threading.Interlocked"),
                                                    &interlocked_type_ref);
                if (FAILED(hr))
                {
                    Logger::Warn("GenerateLoaderType: GetTypeRefOrDefByName iSystem.Threading.Interlocked failed");
                    return hr;
                }
            }

            COR_SIGNATURE interlocked_compare_exchange_signature[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT,
                                                                      3,
                                                                      ELEMENT_TYPE_I4,
                                                                      ELEMENT_TYPE_BYREF,
                                                                      ELEMENT_TYPE_I4,
                                                                      ELEMENT_TYPE_I4,
                                                                      ELEMENT_TYPE_I4};

            hr = resolver.GetMemberRefOrDef(interlocked_type_ref, WStr("CompareExchange"),
                                            interlocked_compare_exchange_signature,
                                            sizeof(interlocked_compare_exchange_signature),
                                            &interlocked_compare_member_ref);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: DefineMemberRef CompareExchange failed");
                return hr;
            }
        }

        // clang-format off
        // IL_0000: ldsflda      int32 __DDVoidMethodType__::_isAssemblyLoaded
        // IL_0005: ldc.i4.1
        // IL_0006: ldc.i4.0
        // IL_0007: call         int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&, int32, int32)
        // IL_000c: ldc.i4.1
        // IL_000d: ceq
        // IL_000f: ret
        // clang-format on
        ILRewriter rewriter_already_loaded(m_pICorProfilerInfo, nullptr, module_id, already_loaded_method_token);
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
            Logger::Warn("GenerateLoaderType: Call to ILRewriter.Export() failed for ModuleID=", module_id);
            return hr;
        }
    }

    // Add IL instructions into __DDVoidMethodCall__
    {
        // MethodRef/MethodDef for
        // instance object [mscorlib]System.Reflection.Assembly::CreateInstance(string)
        mdToken assembly_create_instance_member_ref;
        {
            COR_SIGNATURE assembly_create_instance_signature[] = {IMAGE_CEE_CS_CALLCONV_HASTHIS, 1, ELEMENT_TYPE_OBJECT,
                                                                  ELEMENT_TYPE_STRING};

            hr = resolver.GetMemberRefOrDef(system_reflection_assembly_token, WStr("CreateInstance"),
                                            assembly_create_instance_signature,
                                            sizeof(assembly_create_instance_signature),
                                            &assembly_create_instance_member_ref);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.Reflection.Assembly::CreateInstance failed");
                return hr;
            }
        }

        // Create a string representing "OpenTelemetry.AutoInstrumentation.Loader.Loader"
        mdString load_helper_token;
        {
            LPCWSTR load_helper_str      = L"OpenTelemetry.AutoInstrumentation.Loader.Loader";
            auto    load_helper_str_size = wcslen(load_helper_str);

            hr = metadata_emit->DefineUserString(load_helper_str, (ULONG)load_helper_str_size, &load_helper_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: DefineUserString failed");
                return hr;
            }
        }

        // clang-format off
        // IL_0000: call         bool __DDVoidMethodType__::IsAlreadyLoaded()
        // IL_0005: brfalse.s    IL_0008
        // 
        // IL_0007: ret
        // 
        // IL_0008: ldsfld       class [mscorlib]System.Reflection.Assembly __DDVoidMethodType__::_assembly
        // IL_000d: ldstr        "OpenTelemetry.AutoInstrumentation.Loader.Loader"
        // IL_0012: callvirt     instance object [mscorlib]System.Reflection.Assembly::CreateInstance(string)
        // IL_0017: pop
        // 
        // IL_0018: ret
        // clang-format on
        ILRewriter rewriter_void(m_pICorProfilerInfo, nullptr, module_id, *init_method);
        rewriter_void.InitializeTiny();

        ILInstr* pFirstInstr = rewriter_void.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = already_loaded_method_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_BRFALSE_S;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
        ILInstr* pBranchFalseInstr = pNewInstr;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_RET;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSFLD;
        pNewInstr->m_Arg32  = assembly_field_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
        pBranchFalseInstr->m_pTarget = pNewInstr;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = load_helper_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALLVIRT;
        pNewInstr->m_Arg32  = assembly_create_instance_member_ref;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_POP;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

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
                m_profiler->GetILCodes("*** GenerateLoaderType: Modified Code: ",
                                       &rewriter_void, caller, metadata_import));
        }

        hr = rewriter_void.Export();
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: Call to ILRewriter.Export() failed for ModuleID=", module_id);
            return hr;
        }
    }

    // Add IL instructions into __DDPatchAppDomainSetup__
    {
        // MethodRef/MethodDef for
        // instance void class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup>::Invoke(!0)
        mdToken system_action_of_system_app_domain_setup_invoke_token;
        {
            COR_SIGNATURE system_action_of_system_app_domain_setup_signature[] = {IMAGE_CEE_CS_CALLCONV_HASTHIS, 1,
                                                                                  ELEMENT_TYPE_VOID, ELEMENT_TYPE_VAR,
                                                                                  0};

            hr = resolver.GetMemberRefOrDef(system_action_of_system_app_domain_setup_token, WStr("Invoke"),
                                            system_action_of_system_app_domain_setup_signature,
                                            sizeof(system_action_of_system_app_domain_setup_signature),
                                            &system_action_of_system_app_domain_setup_invoke_token);
            if (FAILED(hr))
            {
                Logger::Warn(
                    "GenerateLoaderType: GetMemberRefOrDef System.Action<System.AppDomainSetup>::Invoke failed");
                return hr;
            }
        }

        // MethodRef/MethodDef for
        // instance void [mscorlib]System.AppDomainSetup::.ctor()
        mdToken system_app_domain_setup_ctor_token;
        {
            COR_SIGNATURE system_app_domain_setup_ctor_signature[] = {IMAGE_CEE_CS_CALLCONV_HASTHIS, 0,
                                                                      ELEMENT_TYPE_VOID};

            hr = resolver.GetMemberRefOrDef(system_app_domain_setup_token, WStr(".ctor"),
                                            system_app_domain_setup_ctor_signature,
                                            sizeof(system_app_domain_setup_ctor_signature),
                                            &system_app_domain_setup_ctor_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateLoaderType: GetMemberRefOrDef System.AppDomainSetup::.ctor failed");
                return hr;
            }
        }

        // clang-format off
        // IL_0000: ldarg.0      // setup
        // IL_0001: ldind.ref
        // IL_0002: brtrue.s     IL_000b
        // 
        // IL_0004: ldarg.0      // setup
        // IL_0005: newobj       instance void [mscorlib]System.AppDomainSetup::.ctor()
        // IL_000a: stind.ref
        // 
        // IL_000b: ldsfld       class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup> __DDVoidMethodType__::_assemblyFixer
        // IL_0010: ldarg.0      // setup
        // IL_0011: ldind.ref
        // IL_0012: callvirt     instance void class [mscorlib]System.Action`1<class [mscorlib]System.AppDomainSetup>::Invoke(!0/*class [mscorlib]System.AppDomainSetup*/)
        // 
        // IL_0017: ret
        // clang-format on
        ILRewriter rewriter_void(m_pICorProfilerInfo, nullptr, module_id, *patch_app_domain_setup_method);
        rewriter_void.InitializeTiny();

        ILInstr* pFirstInstr = rewriter_void.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDIND_REF;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_BRTRUE_S;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
        ILInstr* branch_source = pNewInstr;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_NEWOBJ;
        pNewInstr->m_Arg32  = system_app_domain_setup_ctor_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_STIND_REF;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSFLD;
        pNewInstr->m_Arg32  = app_domain_setup_fixer_field_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);
        branch_source->m_pTarget = pNewInstr;

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_LDIND_REF;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_CALLVIRT;
        pNewInstr->m_Arg32  = system_action_of_system_app_domain_setup_invoke_token;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter_void.NewILInstr();
        pNewInstr->m_opcode = CEE_RET;
        rewriter_void.InsertBefore(pFirstInstr, pNewInstr);

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr("__DDPatchAppDomainSetup__");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(
                m_profiler->GetILCodes("*** GenerateLoaderType: Modified Code: ",
                                       &rewriter_void, caller, metadata_import));
        }

        hr = rewriter_void.Export();
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderType: Call to ILRewriter.Export() failed for ModuleID=", module_id);
            return hr;
        }
    }

    return S_OK;
}

HRESULT StubGenerator::GenerateLoaderMethod(const ModuleID module_id, mdMethodDef* ret_method_token)
{
    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = m_pICorProfilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
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
    hr = GetCorLibAssemblyRef(assembly_emit, m_corAssemblyProperty, &corlib_ref);

    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: failed to define AssemblyRef to mscorlib");
        return hr;
    }

    // Define a TypeRef for Init Type
    mdTypeRef init_type_ref;
    hr = metadata_emit->DefineTypeRefByName(corlib_ref, WStr("__DDVoidMethodType__"), &init_type_ref);
    if (FAILED(hr))
    {
        Logger::Warn("GenerateLoaderMethod: DefineTypeRefByName __DDVoidMethodType__ failed");
        return hr;
    }

    {
        // Define a new static method __DDVoidMethodCall__ on the new type that has a void return type and takes no
        // arguments
        BYTE initialize_signature[] = {
            IMAGE_CEE_CS_CALLCONV_DEFAULT, // Calling convention
            0,                             // Number of parameters
            ELEMENT_TYPE_VOID,             // Return type
        };
        hr = metadata_emit->DefineMemberRef(init_type_ref, WStr("__DDVoidMethodCall__"), initialize_signature,
                                            sizeof(initialize_signature), ret_method_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateLoaderMethod: DefineMemberRef __DDVoidMethodCall__ failed");
            return hr;
        }
    }

    return S_OK;
}

#endif

HRESULT StubGenerator::PatchProcessStartupHooks(const ModuleID module_id, const WSTRING& startup_hook_assembly_path)
{
    mdTypeDef     fixup_type                = mdTokenNil;
    mdMethodDef   patch_startup_hook_method = mdTokenNil;
    HRESULT hr = GenerateHookFixup(module_id, &fixup_type, &patch_startup_hook_method, startup_hook_assembly_path);

    if (FAILED(hr))
    {
        Logger::Error("Failed to inject startup hook patch in System.Private.CoreLib, module id ", module_id,
                      ", result ", hr);
    }
    else
    {
        hr = ModifyProcessStartupHooks(module_id, patch_startup_hook_method);

        if (FAILED(hr))
        {
            Logger::Warn("Failed to patch ProcessStartupHooks injection, module id ", module_id, ", result ", hr);
        }
    }

    return hr;
}

// Add at the start of System.StartupHookProvider::ProcessStartupHooks(string)
// call to __DDLoaderFixup__::__DDPatchStartupHookValue__ passing the startupHooks argument by ref there.
HRESULT StubGenerator::ModifyProcessStartupHooks(const ModuleID module_id, mdMethodDef patch_startup_hook_method)
{
    // Expects to be called on System.Private.CoreLib only
    // patch_startup_hook_method should be pre-injected in System.Private.CoreLib
    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = m_pICorProfilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                         metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("ModifyProcessStartupHooks: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);

    mdTypeDef system_startup_hook_provider_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.StartupHookProvider"), mdTokenNil,
                                                &system_startup_hook_provider_token);
        if (FAILED(hr))
        {
            Logger::Warn("ModifyProcessStartupHooks: FindTypeDefByName System.StartupHookProvider failed");
            return hr;
        }
    }

    mdMethodDef system_startup_hook_provider_process_startup_hooks_token;
    {
        SignatureBuilder::StaticMethod
            system_startup_hook_provider_process_startup_hooks_signature{SignatureBuilder::BuiltIn::Void,
                                                                         {SignatureBuilder::BuiltIn::String}};

        hr = metadata_import->FindMethod(system_startup_hook_provider_token, WStr("ProcessStartupHooks"),
                                         system_startup_hook_provider_process_startup_hooks_signature.Head(),
                                         system_startup_hook_provider_process_startup_hooks_signature.Size(),
                                         &system_startup_hook_provider_process_startup_hooks_token);
        if (FAILED(hr))
        {
            Logger::Warn(
                "ModifyProcessStartupHooks: FindMethod System.StartupHookProvider::ProcessStartupHooks failed");
            return hr;
        }

        ILRewriter rewriter(m_pICorProfilerInfo, nullptr, module_id, system_startup_hook_provider_process_startup_hooks_token);
        hr = rewriter.Import();

        if (FAILED(hr))
        {
            Logger::Warn(
                "ModifyProcessStartupHooks: ILRewriter.Import System.StartupHookProvider::ProcessStartupHooks failed");
            return hr;
        }

        ILInstr* pFirstInstr = rewriter.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        // ldarga.s 0  ; Load the address of the first argument (startupHooks string)
        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARGA_S;
        pNewInstr->m_Arg8   = 0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        // call __DDLoaderFixup__::__DDPatchStartupHookValue__
        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = patch_startup_hook_method;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        hr = rewriter.Export();

        if (FAILED(hr))
        {
            Logger::Warn(
                "ModifyProcessStartupHooks: ILRewriter.Export System.StartupHookProvider::ProcessStartupHooks failed");
            return hr;
        }

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr("ProcessStartupHooks");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(
                m_profiler->GetILCodes("*** ModifyProcessStartupHooks: Modified Code: ", &rewriter, caller, metadata_import));
        }
    }

    return S_OK;
}

// clang-format off
// This method will generate new type __OTLoaderFixup__ in target module.
// C# code for created class:
// public static class __OTLoaderFixup__
// {
//     public static void __OTPatchStartupHookValue__(ref System.String startupHooks)
//     {
//         if (startupHooks == null)
//         {
//             startupHooks = "<startup_hook_dll_name>";
//         }
//         else
//         {
//             startupHooks += ";<startup_hook_dll_name>";
//         }
//     }
// }
// clang-format on
HRESULT StubGenerator::GenerateHookFixup(const ModuleID module_id,
                                       mdTypeDef*     hook_fixup_type,
                                       mdMethodDef*   patch_startup_hook_method,
                                       const WSTRING& startup_hook_dll_name)
{
    const auto& module_info = GetModuleInfo(this->m_pICorProfilerInfo, module_id);
    if (!module_info.IsValid())
    {
        Logger::Warn("GenerateHookFixup: failed to get module info ", module_id);
        return E_FAIL;
    }

    ComPtr<IUnknown> metadata_interfaces;
    auto hr = this->m_pICorProfilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                           metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("GenerateHookFixup: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    const auto& assembly_emit   = metadata_interfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);

    MemberResolver resolver(metadata_import, metadata_emit);

    // TypeDef for System.Object
    mdToken system_object_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.Object"), mdTokenNil, &system_object_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateHookFixup: GetTypeRefOrDefByName System.Object failed");
            return hr;
        }
    }

    // .class public abstract auto ansi sealed __OTLoaderFixup__
    //        extends[mscorlib] System.Object
    {
        hr = metadata_emit->DefineTypeDef(WStr("__OTLoaderFixup__"), tdAbstract | tdSealed | tdPublic,
                                          system_object_token, NULL, hook_fixup_type);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateHookFixup: DefineTypeDef __OTLoaderFixup__ failed");
            return hr;
        }
    }

    // TypeDef/TypeRef for System.String
    mdToken system_string_token;
    {
        hr = metadata_import->FindTypeDefByName(WStr("System.String"), mdTokenNil, &system_string_token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateHookFixup: FindTypeDefByName System.String failed");
            return hr;
        }
    }

    // .method public hidebysig static void  __OTPatchStartupHookValue__(string& startupHooks) cil managed
    {
        SignatureBuilder::StaticMethod patch_startup_hook_method_signature{SignatureBuilder::BuiltIn::Void,
                                                                           {SignatureBuilder::ByRef{
                                                                               SignatureBuilder::BuiltIn::String}}};

        hr = metadata_emit->DefineMethod(*hook_fixup_type, WStr("__OTPatchStartupHookValue__"),
                                         mdPublic | mdHideBySig | mdStatic, patch_startup_hook_method_signature.Head(),
                                         patch_startup_hook_method_signature.Size(), 0, 0, patch_startup_hook_method);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateHookFixup: DefineMethod __OTPatchStartupHookValue__ failed");
            return hr;
        }
    }

    // Add IL instructions into __OTPatchStartupHookValue__
    {
        // MethodRef/MethodDef for string.Concat(string, string)
        mdToken string_concat_token;
        {
            SignatureBuilder::StaticMethod string_concat_signature{SignatureBuilder::BuiltIn::String,
                                                                   {SignatureBuilder::BuiltIn::String,
                                                                    SignatureBuilder::BuiltIn::String}};

            hr = metadata_import->FindMethod(system_string_token, WStr("Concat"), string_concat_signature.Head(),
                                             string_concat_signature.Size(), &string_concat_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateHookFixup: FindMethod System.String::Concat failed");
                return hr;
            }
        }

        // Create a string representing the startup hook DLL name
        mdString startup_hook_dll_token;
        {
            LPCWSTR startup_hook_dll_str      = startup_hook_dll_name.c_str();
            auto    startup_hook_dll_str_size = startup_hook_dll_name.length();

            hr = metadata_emit->DefineUserString(startup_hook_dll_str, (ULONG)startup_hook_dll_str_size,
                                                 &startup_hook_dll_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateHookFixup: DefineUserString failed for startup hook DLL name");
                return hr;
            }
        }

        // Create a string representing a path separator (Unix = ":", Windows = ";")
        mdString path_separator_token;
        {
            const WSTRING path_separator = ENV_VAR_PATH_SEPARATOR_STR;
            LPCWSTR path_separator_str      = path_separator.c_str();
            auto    path_separator_str_size = path_separator.length();

            hr = metadata_emit->DefineUserString(path_separator_str, (ULONG)path_separator_str_size,
                                                 &path_separator_token);
            if (FAILED(hr))
            {
                Logger::Warn("GenerateHookFixup: DefineUserString failed for path separator");
                return hr;
            }
        }

        // Generate IL for the method body
        // C# equivalent:
        // if (startupHooks == null)
        // {
        //     startupHooks = "<startup_hook_dll_name>";
        // }
        // else
        // {
        //     startupHooks += "<path_separator>";
        //     startupHooks += "<startup_hook_dll_name>";
        // }

        // clang-format off
        // IL_0000: ldarg.0
        // IL_0001: ldind.ref
        // IL_0002: brtrue.s     IL_000c
        //
        // IL_0004: ldarg.0
        // IL_0005: ldstr        "<startup_hook_dll_name>"
        // IL_000a: stind.ref
        // IL_000b: ret
        //
        // IL_000c: ldarg.0
        // IL_000d: ldarg.0
        // IL_000e: ldind.ref
        // IL_000f: ldstr        "<path_separator>"
        // IL_0014: call         string [System.Runtime]System.String::Concat(string, string)
        // IL_0019: stind.ref
        //
        // IL_001a: ldarg.0
        // IL_001b: ldarg.0
        // IL_001c: ldind.ref
        // IL_001d: ldstr        "<startup_hook_dll_name>"
        // IL_0022: call         string [System.Runtime]System.String::Concat(string, string)
        // IL_0027: stind.ref
        //
        // IL_0028: ret
        // clang-format on

        ILRewriter rewriter(this->m_pICorProfilerInfo, nullptr, module_id, *patch_startup_hook_method);
        rewriter.InitializeTiny();

        ILInstr* pFirstInstr = rewriter.GetILList()->m_pNext;
        ILInstr* pNewInstr   = NULL;

        // Labels for branching
        ILInstr* elseLabel = NULL;

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        elseLabel            = rewriter.NewILInstr();
        elseLabel->m_opcode  = CEE_NOP; // Placeholder, will be replaced
        pNewInstr            = rewriter.NewILInstr();
        pNewInstr->m_opcode  = CEE_BRTRUE_S;
        pNewInstr->m_pTarget = elseLabel;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = startup_hook_dll_token;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_STIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr            = rewriter.NewILInstr();
        pNewInstr->m_opcode  = CEE_RET;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        // Replace the placeholder else label
        elseLabel->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, elseLabel);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = path_separator_token;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = string_concat_token;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_STIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDARG_0;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_LDSTR;
        pNewInstr->m_Arg32  = startup_hook_dll_token;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_CALL;
        pNewInstr->m_Arg32  = string_concat_token;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_STIND_REF;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_RET;
        rewriter.InsertBefore(pFirstInstr, pNewInstr);

        if (IsDumpILRewriteEnabled())
        {
            mdToken      token = 0;
            TypeInfo     typeInfo{};
            WSTRING      methodName = WStr("__OTPatchStartupHookValue__");
            FunctionInfo caller(token, methodName, typeInfo, MethodSignature(), FunctionMethodSignature());
            Logger::Info(this->m_profiler->GetILCodes("*** GenerateHookFixup: Modified Code: ", &rewriter, caller, metadata_import));
        }

        hr = rewriter.Export();
        if (FAILED(hr))
        {
            Logger::Warn("GenerateHookFixup: Call to ILRewriter.Export() failed for ModuleID=", module_id);
            return hr;
        }
    }

    return S_OK;
}

} // namespace trace
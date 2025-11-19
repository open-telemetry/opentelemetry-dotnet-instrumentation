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
    : m_profiler(profiler), m_pICorProfilerInfo(pICorProfilerInfo), m_corAssemblyProperty(corAssemblyProperty)
{
}

StubGenerator::~StubGenerator() {}

HRESULT StubGenerator::PatchProcessStartupHooks(const ModuleID module_id, const WSTRING& startup_hook_assembly_path)
{
    if (startup_hook_assembly_path.empty())
    {
        return E_INVALIDARG;
    }

    mdTypeDef   fixup_type                = mdTokenNil;
    mdMethodDef patch_startup_hook_method = mdTokenNil;
    HRESULT     hr = GenerateHookFixup(module_id, startup_hook_assembly_path, &fixup_type, &patch_startup_hook_method);

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
// call to __OTLoaderFixup__::__OTPatchStartupHookValue__ passing the startupHooks argument by ref there.
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

        ILRewriter rewriter(m_pICorProfilerInfo, nullptr, module_id,
                            system_startup_hook_provider_process_startup_hooks_token);
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

        // call __OTLoaderFixup__::__OTPatchStartupHookValue__
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
            Logger::Info(m_profiler->GetILCodes("*** ModifyProcessStartupHooks: Modified Code: ", &rewriter, caller,
                                                metadata_import));
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
                                         const WSTRING& startup_hook_dll_name,
                                         mdTypeDef*     hook_fixup_type,
                                         mdMethodDef*   patch_startup_hook_method)
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
            const WSTRING path_separator          = ENV_VAR_PATH_SEPARATOR_STR;
            LPCWSTR       path_separator_str      = path_separator.c_str();
            auto          path_separator_str_size = path_separator.length();

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

        pNewInstr           = rewriter.NewILInstr();
        pNewInstr->m_opcode = CEE_RET;
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
            Logger::Info(this->m_profiler->GetILCodes("*** GenerateHookFixup: Modified Code: ", &rewriter, caller,
                                                      metadata_import));
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
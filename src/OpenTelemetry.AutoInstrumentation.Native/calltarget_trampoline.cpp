/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "calltarget_trampoline.h"

#include <functional>
#include <vector>

#include "clr_helpers.h"
#include "com_ptr.h"
#include "il_rewriter.h"
#include "logger.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"
#include "signature_builder.h"
#include "tracer_tokens.h"
#include "util.h"

namespace trace
{
// clang-format off
// Generates this conceptual C# shape into mscorlib. The emitted IL uses only mscorlib
// metadata; the profiler assembly is found by reflection in each closed generic .cctor.
//
// public sealed class __OTelCallTargetIndexer__<T> { }
//
// public struct __OTelCallTargetState__
// {
//     public object PreviousActivity;
//     public object Activity;
//     public object State;
//     public object StartTime;
//     public static __OTelCallTargetState__ GetDefault() => default;
// }
//
// public struct __OTelCallTargetReturn__
// {
//     public static __OTelCallTargetReturn__ GetDefault() => default;
// }
//
// public struct __OTelCallTargetReturn__<TReturn>
// {
//     private TReturn ReturnValue;
//     public __OTelCallTargetReturn__(TReturn returnValue) { ReturnValue = returnValue; }
//     public static __OTelCallTargetReturn__<TReturn> GetDefault() => default;
//     public TReturn GetReturnValue() => ReturnValue;
// }
//
// public delegate __OTelCallTargetState__ __OTelCallTargetBeginDelegate__<TTarget, ...TArgs>(
//     TTarget instance, ref TArg1 arg1, ..., ref TArgN argN);
// public delegate __OTelCallTargetState__ __OTelCallTargetBeginSlowDelegate__<TTarget>(
//     TTarget instance, object[] args);
// public delegate __OTelCallTargetReturn__<TReturn> __OTelCallTargetEndDelegate__<TTarget, TReturn>(
//     TTarget instance, TReturn returnValue, Exception exception, ref __OTelCallTargetState__ state);
// public delegate __OTelCallTargetReturn__ __OTelCallTargetEndVoidDelegate__<TTarget>(
//     TTarget instance, Exception exception, ref __OTelCallTargetState__ state);
// public delegate void __OTelCallTargetLogExceptionDelegate__(Exception exception);
//
// public static class __OTelCallTargetTrampoline__
// {
//     public static T GetDefaultValue<T>() => default;
//
//     public static __OTelCallTargetState__ BeginMethod<TMapIntegration, TTarget, ...TArgs>(
//         TTarget instance, ref TArg1 arg1, ..., ref TArgN argN) =>
//         __OTelCallTargetTrampolineBegin__<TMapIntegration, TTarget, ...TArgs>
//             .BeginMethod(instance, ref arg1, ..., ref argN);
//
//     public static __OTelCallTargetState__ BeginMethod<TMapIntegration, TTarget>(
//         TTarget instance, object[] args) =>
//         __OTelCallTargetTrampolineBeginSlow__<TMapIntegration, TTarget>
//             .BeginMethod(instance, args);
//
//     public static __OTelCallTargetReturn__<TReturn> EndMethod<TMapIntegration, TTarget, TReturn>(
//         TTarget instance, TReturn returnValue, Exception exception, ref __OTelCallTargetState__ state) =>
//         __OTelCallTargetTrampolineEnd__<TMapIntegration, TTarget, TReturn>
//             .EndMethod(instance, returnValue, exception, ref state);
//
//     public static __OTelCallTargetReturn__ EndMethod<TMapIntegration, TTarget>(
//         TTarget instance, Exception exception, ref __OTelCallTargetState__ state) =>
//         __OTelCallTargetTrampolineEndVoid__<TMapIntegration, TTarget>
//             .EndMethod(instance, exception, ref state);
//
//     public static void LogException<TMapIntegration, TTarget>(Exception exception) =>
//         __OTelCallTargetTrampolineLogException__<TMapIntegration, TTarget>
//             .LogException(exception);
// }
//
// public static class __OTelCallTargetTrampolineBegin__<TMapIntegration, TTarget, ...TArgs>
// {
//     private static __OTelCallTargetBeginDelegate__<TTarget, ...TArgs> _delegate;
//
//     static __OTelCallTargetTrampolineBegin__()
//     {
//         _delegate = (__OTelCallTargetBeginDelegate__<TTarget, ...TArgs>)
//             CallTargetTrampolineInvoker.CreateBeginDelegate(
//                 typeof(TMapIntegration),
//                 typeof(TTarget),
//                 typeof(__OTelCallTargetBeginDelegate__<TTarget, ...TArgs>),
//                 argumentCount);
//     }
//
//     public static __OTelCallTargetState__ BeginMethod(TTarget instance, ref TArg1 arg1, ..., ref TArgN argN) =>
//         _delegate(instance, ref arg1, ..., ref argN);
// }
//
// The slow begin, end, end-void, and log-exception holders follow the same pattern:
// a closed generic static field initialized once by reflection, then direct delegate invocation per call.
// clang-format on
HRESULT GenerateCallTargetTrampolineType(ICorProfilerInfo7* profilerInfo,
                                         const ModuleID module_id,
                                         const WSTRING& profilerAssemblyName)
{
    const auto& module_info = GetModuleInfo(profilerInfo, module_id);
    if (!module_info.IsValid())
    {
        Logger::Warn("GenerateCallTargetTrampolineType: failed to get module info ", module_id);
        return E_FAIL;
    }

    ComPtr<IUnknown> metadata_interfaces;
    auto             hr = profilerInfo->GetModuleMetaData(module_id, ofRead | ofWrite, IID_IMetaDataImport2,
                                                         metadata_interfaces.GetAddressOf());
    if (FAILED(hr))
    {
        Logger::Warn("GenerateCallTargetTrampolineType: failed to get metadata interface for ", module_id);
        return hr;
    }

    const auto& metadata_import = metadata_interfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
    const auto& metadata_emit   = metadata_interfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
    MemberResolver resolver(metadata_import, metadata_emit);

    const mdAssemblyRef corlib_ref = mdTokenNil;
    auto get_corlib_type = [&](LPCWSTR name, mdToken* token) -> HRESULT {
        hr = resolver.GetTypeRefOrDefByName(corlib_ref, name, token);
        if (FAILED(hr))
        {
            Logger::Warn("GenerateCallTargetTrampolineType: failed to resolve ", WSTRING(name));
        }
        return hr;
    };

    mdToken system_object_token, system_exception_token, system_type_token, system_delegate_token;
    mdToken system_multicast_delegate_token, system_assembly_token, system_method_info_token, system_method_base_token;
    mdToken system_binding_flags_token, system_int32_token, system_value_type_token;
    IfFailRet(get_corlib_type(SystemObject, &system_object_token));
    IfFailRet(get_corlib_type(SystemException, &system_exception_token));
    IfFailRet(get_corlib_type(SystemTypeName, &system_type_token));
    IfFailRet(get_corlib_type(WStr("System.Delegate"), &system_delegate_token));
    IfFailRet(get_corlib_type(WStr("System.MulticastDelegate"), &system_multicast_delegate_token));
    IfFailRet(get_corlib_type(WStr("System.Reflection.Assembly"), &system_assembly_token));
    IfFailRet(get_corlib_type(WStr("System.Reflection.MethodInfo"), &system_method_info_token));
    IfFailRet(get_corlib_type(WStr("System.Reflection.MethodBase"), &system_method_base_token));
    IfFailRet(get_corlib_type(WStr("System.Reflection.BindingFlags"), &system_binding_flags_token));
    IfFailRet(get_corlib_type(WStr("System.Int32"), &system_int32_token));
    IfFailRet(get_corlib_type(WStr("System.ValueType"), &system_value_type_token));

    SignatureBuilder::Type object_type{SignatureBuilder::BuiltIn::Object};
    SignatureBuilder::Class type_class{system_type_token};
    SignatureBuilder::ValueType binding_flags_value{system_binding_flags_token};
    SignatureBuilder::Array object_array{object_type};

    auto append_compressed_data = [](std::vector<COR_SIGNATURE>& signature, ULONG value) {
        COR_SIGNATURE buffer[sizeof(ULONG)];
        ULONG size = CorSigCompressData(value, buffer);
        signature.insert(signature.end(), buffer, buffer + size);
    };
    auto append_token = [](std::vector<COR_SIGNATURE>& signature, mdToken token) {
        COR_SIGNATURE buffer[sizeof(mdToken)];
        ULONG size = CorSigCompressToken(token, buffer);
        signature.insert(signature.end(), buffer, buffer + size);
    };
    auto append_type_token = [&](std::vector<COR_SIGNATURE>& signature, mdToken token, bool is_value_type) {
        signature.push_back(is_value_type ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);
        append_token(signature, token);
    };
    auto append_var = [&](std::vector<COR_SIGNATURE>& signature, ULONG index, bool by_ref = false) {
        if (by_ref)
        {
            signature.push_back(ELEMENT_TYPE_BYREF);
        }
        signature.push_back(ELEMENT_TYPE_VAR);
        append_compressed_data(signature, index);
    };
    auto append_mvar = [&](std::vector<COR_SIGNATURE>& signature, ULONG index, bool by_ref = false) {
        if (by_ref)
        {
            signature.push_back(ELEMENT_TYPE_BYREF);
        }
        signature.push_back(ELEMENT_TYPE_MVAR);
        append_compressed_data(signature, index);
    };
    auto field_signature = [&](const std::vector<COR_SIGNATURE>& field_type) {
        std::vector<COR_SIGNATURE> signature{IMAGE_CEE_CS_CALLCONV_FIELD};
        signature.insert(signature.end(), field_type.begin(), field_type.end());
        return signature;
    };
    auto static_method_signature = [&](const std::vector<COR_SIGNATURE>& return_type,
                                       const std::vector<std::vector<COR_SIGNATURE>>& args) {
        std::vector<COR_SIGNATURE> signature{IMAGE_CEE_CS_CALLCONV_DEFAULT};
        append_compressed_data(signature, static_cast<ULONG>(args.size()));
        signature.insert(signature.end(), return_type.begin(), return_type.end());
        for (const auto& arg : args)
        {
            signature.insert(signature.end(), arg.begin(), arg.end());
        }
        return signature;
    };
    auto generic_static_method_signature = [&](ULONG generic_arg_count,
                                               const std::vector<COR_SIGNATURE>& return_type,
                                               const std::vector<std::vector<COR_SIGNATURE>>& args) {
        std::vector<COR_SIGNATURE> signature{IMAGE_CEE_CS_CALLCONV_GENERIC};
        append_compressed_data(signature, generic_arg_count);
        append_compressed_data(signature, static_cast<ULONG>(args.size()));
        signature.insert(signature.end(), return_type.begin(), return_type.end());
        for (const auto& arg : args)
        {
            signature.insert(signature.end(), arg.begin(), arg.end());
        }
        return signature;
    };
    auto instance_method_signature = [&](const std::vector<COR_SIGNATURE>& return_type,
                                         const std::vector<std::vector<COR_SIGNATURE>>& args) {
        auto signature = static_method_signature(return_type, args);
        signature[0] |= IMAGE_CEE_CS_CALLCONV_HASTHIS;
        return signature;
    };
    auto generic_instance_signature = [&](mdToken open_type, bool is_value_type,
                                          const std::vector<std::vector<COR_SIGNATURE>>& args) {
        std::vector<COR_SIGNATURE> signature{ELEMENT_TYPE_GENERICINST};
        append_type_token(signature, open_type, is_value_type);
        append_compressed_data(signature, static_cast<ULONG>(args.size()));
        for (const auto& arg : args)
        {
            signature.insert(signature.end(), arg.begin(), arg.end());
        }
        return signature;
    };
    auto get_type_spec = [&](const std::vector<COR_SIGNATURE>& signature, mdTypeSpec* token) -> HRESULT {
        return metadata_emit->GetTokenFromTypeSpec(signature.data(), static_cast<ULONG>(signature.size()), token);
    };
    auto define_generic_params = [&](mdToken owner, const std::vector<WSTRING>& names) -> HRESULT {
        for (ULONG i = 0; i < names.size(); i++)
        {
            mdGenericParam generic_param;
            IfFailRet(metadata_emit->DefineGenericParam(owner, i, gpNonVariant, names[i].c_str(), 0, nullptr,
                                                        &generic_param));
        }
        return S_OK;
    };

    auto make_var_type_spec = [&](ULONG index, mdTypeSpec* token) -> HRESULT {
        std::vector<COR_SIGNATURE> signature;
        append_var(signature, index);
        return get_type_spec(signature, token);
    };

    auto make_mvar_type_spec = [&](ULONG index, mdTypeSpec* token) -> HRESULT {
        std::vector<COR_SIGNATURE> signature;
        append_mvar(signature, index);
        return get_type_spec(signature, token);
    };

    auto make_generic_type_spec = [&](mdToken open_type, bool is_value_type,
                                      const std::vector<std::vector<COR_SIGNATURE>>& args,
                                      mdTypeSpec* token) -> HRESULT {
        auto signature = generic_instance_signature(open_type, is_value_type, args);
        return get_type_spec(signature, token);
    };

    auto set_single_local_sig = [&](ILRewriter& rewriter, const std::vector<COR_SIGNATURE>& local_type) -> HRESULT {
        std::vector<COR_SIGNATURE> signature{IMAGE_CEE_CS_CALLCONV_LOCAL_SIG};
        append_compressed_data(signature, 1);
        signature.insert(signature.end(), local_type.begin(), local_type.end());

        mdSignature localSignatureToken;
        IfFailRet(metadata_emit->GetTokenFromSig(signature.data(), static_cast<ULONG>(signature.size()),
                                                 &localSignatureToken));
        rewriter.SetTkLocalVarSig(localSignatureToken);
        return S_OK;
    };

    std::vector<COR_SIGNATURE> void_sig{ELEMENT_TYPE_VOID};
    std::vector<COR_SIGNATURE> object_sig{ELEMENT_TYPE_OBJECT};
    std::vector<COR_SIGNATURE> exception_sig;
    append_type_token(exception_sig, system_exception_token, false);
    std::vector<COR_SIGNATURE> type_sig;
    append_type_token(type_sig, system_type_token, false);
    std::vector<COR_SIGNATURE> delegate_sig;
    append_type_token(delegate_sig, system_delegate_token, false);

    mdMemberRef type_get_type_from_handle, assembly_load, assembly_get_type, type_get_method, method_base_invoke;
    {
        mdToken runtime_type_handle_token;
        IfFailRet(get_corlib_type(RuntimeTypeHandleTypeName, &runtime_type_handle_token));
        SignatureBuilder::StaticMethod signature{type_class, {SignatureBuilder::ValueType{runtime_type_handle_token}}};
        IfFailRet(resolver.GetMemberRefOrDef(system_type_token, GetTypeFromHandleMethodName, signature.Head(),
                                             signature.Size(), &type_get_type_from_handle));
    }
    {
        SignatureBuilder::StaticMethod signature{SignatureBuilder::Class{system_assembly_token},
                                                 {SignatureBuilder::Type{SignatureBuilder::BuiltIn::String}}};
        IfFailRet(resolver.GetMemberRefOrDef(system_assembly_token, WStr("Load"), signature.Head(), signature.Size(),
                                             &assembly_load));
    }
    {
        SignatureBuilder::InstanceMethod signature{type_class, {SignatureBuilder::Type{SignatureBuilder::BuiltIn::String},
                                                                SignatureBuilder::BuiltIn::Boolean}};
        IfFailRet(resolver.GetMemberRefOrDef(system_assembly_token, WStr("GetType"), signature.Head(),
                                             signature.Size(), &assembly_get_type));
    }
    {
        SignatureBuilder::InstanceMethod signature{SignatureBuilder::Class{system_method_info_token},
                                                   {SignatureBuilder::Type{SignatureBuilder::BuiltIn::String},
                                                    binding_flags_value}};
        IfFailRet(resolver.GetMemberRefOrDef(system_type_token, WStr("GetMethod"), signature.Head(),
                                             signature.Size(), &type_get_method));
    }
    {
        SignatureBuilder::InstanceMethod signature{object_type, {object_type, object_array}};
        IfFailRet(resolver.GetMemberRefOrDef(system_method_base_token, WStr("Invoke"), signature.Head(),
                                             signature.Size(), &method_base_invoke));
    }

    auto emit_arg = [](ILRewriter& rewriter, ILInstr* position, UINT16 index) {
        static const std::vector<OPCODE> opcodes = {CEE_LDARG_0, CEE_LDARG_1, CEE_LDARG_2, CEE_LDARG_3};
        ILInstr* instr = rewriter.NewILInstr();
        if (index <= 3) { instr->m_opcode = opcodes[index]; }
        else { instr->m_opcode = CEE_LDARG_S; instr->m_Arg8 = static_cast<UINT8>(index); }
        rewriter.InsertBefore(position, instr);
        return instr;
    };
    auto emit_i4 = [](ILRewriter& rewriter, ILInstr* position, INT32 value) {
        static const std::vector<OPCODE> opcodes = {CEE_LDC_I4_0, CEE_LDC_I4_1, CEE_LDC_I4_2, CEE_LDC_I4_3,
                                                    CEE_LDC_I4_4, CEE_LDC_I4_5, CEE_LDC_I4_6, CEE_LDC_I4_7,
                                                    CEE_LDC_I4_8};
        ILInstr* instr = rewriter.NewILInstr();
        if (value >= 0 && value <= 8) { instr->m_opcode = opcodes[value]; }
        else { instr->m_opcode = CEE_LDC_I4_S; instr->m_Arg8 = static_cast<INT8>(value); }
        rewriter.InsertBefore(position, instr);
        return instr;
    };
    auto emit_token = [](ILRewriter& rewriter, ILInstr* position, OPCODE op_code, mdToken token) {
        ILInstr* instr = rewriter.NewILInstr(); instr->m_opcode = op_code; instr->m_Arg32 = token;
        rewriter.InsertBefore(position, instr); return instr;
    };
    auto emit_simple = [](ILRewriter& rewriter, ILInstr* position, OPCODE op_code) {
        ILInstr* instr = rewriter.NewILInstr(); instr->m_opcode = op_code;
        rewriter.InsertBefore(position, instr); return instr;
    };
    auto emit_string = [&](ILRewriter& rewriter, ILInstr* position, const WSTRING& value) {
        mdString token;
        metadata_emit->DefineUserString(value.c_str(), static_cast<ULONG>(value.size()), &token);
        return emit_token(rewriter, position, CEE_LDSTR, token);
    };
    auto emit_type_from_token = [&](ILRewriter& rewriter, ILInstr* position, mdToken token) {
        emit_token(rewriter, position, CEE_LDTOKEN, token);
        emit_token(rewriter, position, CEE_CALL, type_get_type_from_handle);
    };
    auto emit_array_item = [&](ILRewriter& rewriter, ILInstr* position, INT32 index) {
        emit_simple(rewriter, position, CEE_DUP);
        emit_i4(rewriter, position, index);
    };
    auto export_method = [&](mdMethodDef method, ILRewriter& rewriter) -> HRESULT {
        hr = rewriter.Export();
        if (FAILED(hr)) { Logger::Warn("GenerateCallTargetTrampolineType: ILRewriter.Export failed for method ", method); }
        return hr;
    };

    auto define_static_default_method = [&](mdTypeDef type,
                                            const WSTRING& methodName,
                                            const std::vector<COR_SIGNATURE>& returnType,
                                            mdToken initObjToken) -> HRESULT {
        auto methodSig = static_method_signature(returnType, {});
        mdMethodDef method;
        IfFailRet(metadata_emit->DefineMethod(type, methodName.c_str(), mdPublic | mdHideBySig | mdStatic,
                                              methodSig.data(), static_cast<ULONG>(methodSig.size()), 0, 0, &method));

        ILRewriter rewriter(profilerInfo, nullptr, module_id, method);
        rewriter.InitializeTiny();
        IfFailRet(set_single_local_sig(rewriter, returnType));

        ILInstr* p = rewriter.GetILList()->m_pNext;
        emit_simple(rewriter, p, CEE_LDLOCA_S)->m_Arg8 = 0;
        emit_token(rewriter, p, CEE_INITOBJ, initObjToken);
        emit_simple(rewriter, p, CEE_LDLOC_0);
        emit_simple(rewriter, p, CEE_RET);
        return export_method(method, rewriter);
    };

    const WSTRING factoryTypeName = WStr("OpenTelemetry.AutoInstrumentation.CallTarget.CallTargetTrampolineInvoker");
    auto emit_factory_call = [&](ILRewriter& rewriter,
                                 ILInstr* position,
                                 const WSTRING& factoryMethodName,
                                 const std::vector<std::function<void()>>& emitArguments) {
        emit_string(rewriter, position, profilerAssemblyName);
        emit_token(rewriter, position, CEE_CALL, assembly_load);
        emit_string(rewriter, position, factoryTypeName);
        emit_i4(rewriter, position, 1);
        emit_token(rewriter, position, CEE_CALLVIRT, assembly_get_type);
        emit_string(rewriter, position, factoryMethodName);
        emit_i4(rewriter, position, 24);
        emit_token(rewriter, position, CEE_CALLVIRT, type_get_method);
        emit_simple(rewriter, position, CEE_LDNULL);
        emit_i4(rewriter, position, static_cast<INT32>(emitArguments.size()));
        emit_token(rewriter, position, CEE_NEWARR, system_object_token);
        for (INT32 i = 0; i < static_cast<INT32>(emitArguments.size()); i++)
        {
            emit_array_item(rewriter, position, i);
            emitArguments[i]();
            emit_simple(rewriter, position, CEE_STELEM_REF);
        }
        emit_token(rewriter, position, CEE_CALLVIRT, method_base_invoke);
    };

    mdTypeDef indexer_type;
    IfFailRet(metadata_emit->DefineTypeDef(WStr("__OTelCallTargetIndexer`1"), tdPublic, system_object_token, nullptr,
                                           &indexer_type));
    IfFailRet(define_generic_params(indexer_type, {WStr("T")}));

    mdTypeDef state_type;
    IfFailRet(metadata_emit->DefineTypeDef(WStr("__OTelCallTargetState__"),
                                           tdPublic | tdSequentialLayout | tdSealed, system_value_type_token, nullptr,
                                           &state_type));
    {
        auto objectFieldSig = field_signature(object_sig);
        mdFieldDef field;
        IfFailRet(metadata_emit->DefineField(state_type, WStr("PreviousActivity"), fdPublic, objectFieldSig.data(),
                                             static_cast<ULONG>(objectFieldSig.size()), 0, nullptr, 0, &field));
        IfFailRet(metadata_emit->DefineField(state_type, WStr("Activity"), fdPublic, objectFieldSig.data(),
                                             static_cast<ULONG>(objectFieldSig.size()), 0, nullptr, 0, &field));
        IfFailRet(metadata_emit->DefineField(state_type, WStr("State"), fdPublic, objectFieldSig.data(),
                                             static_cast<ULONG>(objectFieldSig.size()), 0, nullptr, 0, &field));
        IfFailRet(metadata_emit->DefineField(state_type, WStr("StartTime"), fdPublic, objectFieldSig.data(),
                                             static_cast<ULONG>(objectFieldSig.size()), 0, nullptr, 0, &field));
    }
    {
        std::vector<COR_SIGNATURE> stateReturnSig;
        append_type_token(stateReturnSig, state_type, true);
        IfFailRet(define_static_default_method(state_type, WStr("GetDefault"), stateReturnSig, state_type));
    }

    mdTypeDef return_void_type;
    IfFailRet(metadata_emit->DefineTypeDef(WStr("__OTelCallTargetReturn__"), tdPublic | tdSequentialLayout | tdSealed,
                                           system_value_type_token, nullptr, &return_void_type));
    {
        std::vector<COR_SIGNATURE> returnVoidSig;
        append_type_token(returnVoidSig, return_void_type, true);
        IfFailRet(define_static_default_method(return_void_type, WStr("GetDefault"), returnVoidSig, return_void_type));
    }

    mdTypeDef return_generic_type;
    mdFieldDef return_value_field;
    IfFailRet(metadata_emit->DefineTypeDef(WStr("__OTelCallTargetReturn__`1"),
                                           tdPublic | tdSequentialLayout | tdSealed, system_value_type_token, nullptr,
                                           &return_generic_type));
    IfFailRet(define_generic_params(return_generic_type, {WStr("TReturn")}));
    {
        std::vector<COR_SIGNATURE> returnValueSig;
        append_var(returnValueSig, 0);
        auto fieldSig = field_signature(returnValueSig);
        IfFailRet(metadata_emit->DefineField(return_generic_type, WStr("ReturnValue"), fdPrivate, fieldSig.data(),
                                             static_cast<ULONG>(fieldSig.size()), 0, nullptr, 0, &return_value_field));
    }
    mdMethodDef return_ctor, return_generic_get_default, get_return_value_method;
    {
        std::vector<COR_SIGNATURE> returnVar;
        append_var(returnVar, 0);
        auto returnGenericSig = generic_instance_signature(return_generic_type, true, {returnVar});
        mdTypeSpec returnGenericTypeSpec;
        IfFailRet(make_generic_type_spec(return_generic_type, true, {returnVar}, &returnGenericTypeSpec));

        auto getDefaultSig = static_method_signature(returnGenericSig, {});
        IfFailRet(metadata_emit->DefineMethod(return_generic_type, WStr("GetDefault"), mdPublic | mdHideBySig | mdStatic,
                                              getDefaultSig.data(), static_cast<ULONG>(getDefaultSig.size()), 0, 0,
                                              &return_generic_get_default));
        ILRewriter defaultRewriter(profilerInfo, nullptr, module_id, return_generic_get_default);
        defaultRewriter.InitializeTiny();
        IfFailRet(set_single_local_sig(defaultRewriter, returnGenericSig));
        ILInstr* p = defaultRewriter.GetILList()->m_pNext;
        emit_simple(defaultRewriter, p, CEE_LDLOCA_S)->m_Arg8 = 0;
        emit_token(defaultRewriter, p, CEE_INITOBJ, returnGenericTypeSpec);
        emit_simple(defaultRewriter, p, CEE_LDLOC_0);
        emit_simple(defaultRewriter, p, CEE_RET);
        IfFailRet(export_method(return_generic_get_default, defaultRewriter));

        auto ctorSig = instance_method_signature(void_sig, {returnVar});
        IfFailRet(metadata_emit->DefineMethod(return_generic_type, WStr(".ctor"),
                                              mdPublic | mdHideBySig | mdSpecialName | mdRTSpecialName, ctorSig.data(),
                                              static_cast<ULONG>(ctorSig.size()), 0, 0, &return_ctor));
        ILRewriter rewriter(profilerInfo, nullptr, module_id, return_ctor);
        rewriter.InitializeTiny();
        p = rewriter.GetILList()->m_pNext;
        emit_arg(rewriter, p, 0);
        emit_arg(rewriter, p, 1);
        emit_token(rewriter, p, CEE_STFLD, return_value_field);
        emit_simple(rewriter, p, CEE_RET);
        IfFailRet(export_method(return_ctor, rewriter));

        auto getSig = instance_method_signature(returnVar, {});
        IfFailRet(metadata_emit->DefineMethod(return_generic_type, WStr("GetReturnValue"),
                                              mdPublic | mdHideBySig, getSig.data(),
                                              static_cast<ULONG>(getSig.size()), 0, 0, &get_return_value_method));
        ILRewriter getRewriter(profilerInfo, nullptr, module_id, get_return_value_method);
        getRewriter.InitializeTiny();
        p = getRewriter.GetILList()->m_pNext;
        emit_arg(getRewriter, p, 0);
        emit_token(getRewriter, p, CEE_LDFLD, return_value_field);
        emit_simple(getRewriter, p, CEE_RET);
        IfFailRet(export_method(get_return_value_method, getRewriter));
    }

    std::vector<COR_SIGNATURE> state_sig;
    append_type_token(state_sig, state_type, true);
    std::vector<COR_SIGNATURE> return_void_sig;
    append_type_token(return_void_sig, return_void_type, true);

    mdTypeDef trampoline_type;
    IfFailRet(metadata_emit->DefineTypeDef(WStr("__OTelCallTargetTrampoline__"),
                                           tdPublic | tdAbstract | tdSealed, system_object_token, nullptr,
                                           &trampoline_type));
    {
        std::vector<COR_SIGNATURE> returnMethodVar;
        append_mvar(returnMethodVar, 0);
        auto getDefaultValueSig = generic_static_method_signature(1, returnMethodVar, {});
        mdMethodDef getDefaultValueMethod;
        IfFailRet(metadata_emit->DefineMethod(trampoline_type, WStr("GetDefaultValue"),
                                              mdPublic | mdHideBySig | mdStatic, getDefaultValueSig.data(),
                                              static_cast<ULONG>(getDefaultValueSig.size()), 0, 0,
                                              &getDefaultValueMethod));
        IfFailRet(define_generic_params(getDefaultValueMethod, {WStr("T")}));

        mdTypeSpec returnMethodVarTypeSpec;
        IfFailRet(make_mvar_type_spec(0, &returnMethodVarTypeSpec));

        ILRewriter rewriter(profilerInfo, nullptr, module_id, getDefaultValueMethod);
        rewriter.InitializeTiny();
        IfFailRet(set_single_local_sig(rewriter, returnMethodVar));
        ILInstr* p = rewriter.GetILList()->m_pNext;
        emit_simple(rewriter, p, CEE_LDLOCA_S)->m_Arg8 = 0;
        emit_token(rewriter, p, CEE_INITOBJ, returnMethodVarTypeSpec);
        emit_simple(rewriter, p, CEE_LDLOC_0);
        emit_simple(rewriter, p, CEE_RET);
        IfFailRet(export_method(getDefaultValueMethod, rewriter));
    }

    auto define_delegate_type = [&](const WSTRING& name,
                                    const std::vector<WSTRING>& genericNames,
                                    const std::vector<COR_SIGNATURE>& returnType,
                                    const std::vector<std::vector<COR_SIGNATURE>>& parameters,
                                    mdTypeDef* delegateType) -> HRESULT {
        IfFailRet(metadata_emit->DefineTypeDef(name.c_str(), tdPublic | tdSealed, system_multicast_delegate_token,
                                               nullptr, delegateType));
        IfFailRet(define_generic_params(*delegateType, genericNames));

        auto ctorSig = instance_method_signature(void_sig, {object_sig, {ELEMENT_TYPE_I}});
        mdMethodDef ctor;
        IfFailRet(metadata_emit->DefineMethod(*delegateType, WStr(".ctor"),
                                              mdPublic | mdHideBySig | mdSpecialName | mdRTSpecialName, ctorSig.data(),
                                              static_cast<ULONG>(ctorSig.size()), 0, 0, &ctor));
        IfFailRet(metadata_emit->SetMethodImplFlags(ctor, miRuntime | miManaged));

        auto invokeSig = instance_method_signature(returnType, parameters);
        mdMethodDef invoke;
        IfFailRet(metadata_emit->DefineMethod(*delegateType, WStr("Invoke"),
                                              mdPublic | mdHideBySig | mdNewSlot | mdVirtual, invokeSig.data(),
                                              static_cast<ULONG>(invokeSig.size()), 0, 0, &invoke));
        IfFailRet(metadata_emit->SetMethodImplFlags(invoke, miRuntime | miManaged));
        return S_OK;
    };

    mdTypeDef begin_delegate_types[9];
    for (int arity = 0; arity < FASTPATH_COUNT; arity++)
    {
        std::vector<WSTRING> genericNames{WStr("TTarget")};
        std::vector<std::vector<COR_SIGNATURE>> parameters;
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 0);
        parameters.push_back(targetArg);
        for (int i = 0; i < arity; i++)
        {
            genericNames.push_back(WStr("TArg") + ToWSTRING(i + 1));
            std::vector<COR_SIGNATURE> arg;
            append_var(arg, static_cast<ULONG>(i + 1), true);
            parameters.push_back(arg);
        }

        const auto name = WStr("__OTelCallTargetBeginDelegate__`") + ToWSTRING(arity + 1);
        IfFailRet(define_delegate_type(name, genericNames, state_sig, parameters, &begin_delegate_types[arity]));
    }

    mdTypeDef begin_slow_delegate_type, end_delegate_type, end_void_delegate_type, log_exception_delegate_type;
    {
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 0);
        std::vector<COR_SIGNATURE> objectArrayArg{ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_OBJECT};
        IfFailRet(define_delegate_type(WStr("__OTelCallTargetBeginSlowDelegate__`1"), {WStr("TTarget")}, state_sig,
                                       {targetArg, objectArrayArg}, &begin_slow_delegate_type));
    }
    {
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 0);
        std::vector<COR_SIGNATURE> returnArg;
        append_var(returnArg, 1);
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        auto returnType = generic_instance_signature(return_generic_type, true, {returnArg});
        IfFailRet(define_delegate_type(WStr("__OTelCallTargetEndDelegate__`2"), {WStr("TTarget"), WStr("TReturn")},
                                       returnType, {targetArg, returnArg, exception_sig, stateByRef},
                                       &end_delegate_type));
    }
    {
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 0);
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        IfFailRet(define_delegate_type(WStr("__OTelCallTargetEndVoidDelegate__`1"), {WStr("TTarget")},
                                       return_void_sig, {targetArg, exception_sig, stateByRef},
                                       &end_void_delegate_type));
    }
    IfFailRet(define_delegate_type(WStr("__OTelCallTargetLogExceptionDelegate__"), {}, void_sig, {exception_sig},
                                   &log_exception_delegate_type));

    auto define_holder_type = [&](const WSTRING& name,
                                  const std::vector<WSTRING>& genericNames,
                                  mdTypeDef* holderType) -> HRESULT {
        IfFailRet(metadata_emit->DefineTypeDef(name.c_str(), tdPublic | tdAbstract | tdSealed, system_object_token,
                                               nullptr, holderType));
        return define_generic_params(*holderType, genericNames);
    };

    auto define_static_delegate_field = [&](mdTypeDef holderType,
                                            mdToken delegateType,
                                            const std::vector<std::vector<COR_SIGNATURE>>& delegateArgs,
                                            mdFieldDef* field,
                                            mdTypeSpec* delegateTypeSpec) -> HRESULT {
        IfFailRet(make_generic_type_spec(delegateType, false, delegateArgs, delegateTypeSpec));
        PCCOR_SIGNATURE delegateSig = nullptr;
        ULONG delegateSigSize = 0;
        IfFailRet(metadata_import->GetTypeSpecFromToken(*delegateTypeSpec, &delegateSig, &delegateSigSize));
        std::vector<COR_SIGNATURE> fieldSig{IMAGE_CEE_CS_CALLCONV_FIELD};
        fieldSig.insert(fieldSig.end(), delegateSig, delegateSig + delegateSigSize);
        return metadata_emit->DefineField(holderType, WStr("_delegate"), fdPrivate | fdStatic, fieldSig.data(),
                                          static_cast<ULONG>(fieldSig.size()), 0, nullptr, 0, field);
    };

    auto define_holder_cctor = [&](mdTypeDef holderType,
                                   mdFieldDef field,
                                   mdTypeSpec delegateTypeSpec,
                                   const WSTRING& factoryMethod,
                                   const std::vector<mdToken>& typeTokens,
                                   int arity) -> HRESULT {
        auto cctorSig = static_method_signature(void_sig, {});
        mdMethodDef cctor;
        IfFailRet(metadata_emit->DefineMethod(holderType, WStr(".cctor"),
                                              mdPrivate | mdHideBySig | mdSpecialName | mdRTSpecialName | mdStatic,
                                              cctorSig.data(), static_cast<ULONG>(cctorSig.size()), 0, 0, &cctor));
        ILRewriter rewriter(profilerInfo, nullptr, module_id, cctor);
        rewriter.InitializeTiny();
        ILInstr* p = rewriter.GetILList()->m_pNext;
        std::vector<std::function<void()>> args;
        for (auto token : typeTokens)
        {
            args.push_back([&, token]() { emit_type_from_token(rewriter, p, token); });
        }
        if (arity >= 0)
        {
            args.push_back([&]() {
                emit_i4(rewriter, p, arity);
                emit_token(rewriter, p, CEE_BOX, system_int32_token);
            });
        }
        emit_factory_call(rewriter, p, factoryMethod, args);
        emit_token(rewriter, p, CEE_CASTCLASS, delegateTypeSpec);
        emit_token(rewriter, p, CEE_STSFLD, field);
        emit_simple(rewriter, p, CEE_RET);
        return export_method(cctor, rewriter);
    };

    auto define_holder_method = [&](mdTypeDef holderType,
                                    mdFieldDef field,
                                    mdTypeSpec delegateTypeSpec,
                                    const WSTRING& methodName,
                                    const std::vector<COR_SIGNATURE>& returnType,
                                    const std::vector<std::vector<COR_SIGNATURE>>& parameters) -> HRESULT {
        auto methodSig = static_method_signature(returnType, parameters);
        mdMethodDef method;
        IfFailRet(metadata_emit->DefineMethod(holderType, methodName.c_str(), mdPublic | mdHideBySig | mdStatic,
                                              methodSig.data(), static_cast<ULONG>(methodSig.size()), 0, 0, &method));
        auto invokeSig = instance_method_signature(returnType, parameters);
        mdMemberRef invokeRef;
        IfFailRet(metadata_emit->DefineMemberRef(delegateTypeSpec, WStr("Invoke"), invokeSig.data(),
                                                 static_cast<ULONG>(invokeSig.size()), &invokeRef));
        ILRewriter rewriter(profilerInfo, nullptr, module_id, method);
        rewriter.InitializeTiny();
        ILInstr* p = rewriter.GetILList()->m_pNext;
        emit_token(rewriter, p, CEE_LDSFLD, field);
        for (UINT16 i = 0; i < parameters.size(); i++)
        {
            emit_arg(rewriter, p, i);
        }
        emit_token(rewriter, p, CEE_CALLVIRT, invokeRef);
        emit_simple(rewriter, p, CEE_RET);
        return export_method(method, rewriter);
    };

    auto define_trampoline_forwarder =
        [&](const WSTRING& methodName,
            const std::vector<WSTRING>& genericNames,
            const std::vector<COR_SIGNATURE>& returnType,
            const std::vector<std::vector<COR_SIGNATURE>>& parameters,
            mdTypeDef holderOpenType,
            const std::vector<std::vector<COR_SIGNATURE>>& holderTypeArguments,
            const WSTRING& holderMethodName,
            const std::vector<COR_SIGNATURE>& holderReturnType,
            const std::vector<std::vector<COR_SIGNATURE>>& holderParameters) -> HRESULT {
        auto methodSig = generic_static_method_signature(static_cast<ULONG>(genericNames.size()), returnType,
                                                         parameters);
        mdMethodDef method;
        IfFailRet(metadata_emit->DefineMethod(trampoline_type, methodName.c_str(), mdPublic | mdHideBySig | mdStatic,
                                              methodSig.data(), static_cast<ULONG>(methodSig.size()), 0, 0, &method));
        IfFailRet(define_generic_params(method, genericNames));

        mdTypeSpec holderTypeSpec;
        IfFailRet(make_generic_type_spec(holderOpenType, false, holderTypeArguments, &holderTypeSpec));

        auto holderMethodSig = static_method_signature(holderReturnType, holderParameters);
        mdMemberRef holderMethodRef;
        IfFailRet(metadata_emit->DefineMemberRef(holderTypeSpec, holderMethodName.c_str(), holderMethodSig.data(),
                                                 static_cast<ULONG>(holderMethodSig.size()), &holderMethodRef));

        ILRewriter rewriter(profilerInfo, nullptr, module_id, method);
        rewriter.InitializeTiny();
        ILInstr* p = rewriter.GetILList()->m_pNext;
        for (UINT16 i = 0; i < parameters.size(); i++)
        {
            emit_arg(rewriter, p, i);
        }
        emit_token(rewriter, p, CEE_CALL, holderMethodRef);
        emit_simple(rewriter, p, CEE_RET);
        return export_method(method, rewriter);
    };

    auto var_signature = [&](ULONG index, bool by_ref = false) {
        std::vector<COR_SIGNATURE> signature;
        append_var(signature, index, by_ref);
        return signature;
    };
    auto mvar_signature = [&](ULONG index, bool by_ref = false) {
        std::vector<COR_SIGNATURE> signature;
        append_mvar(signature, index, by_ref);
        return signature;
    };

    mdTypeSpec var0, var1, var2;
    IfFailRet(make_var_type_spec(0, &var0));
    IfFailRet(make_var_type_spec(1, &var1));
    IfFailRet(make_var_type_spec(2, &var2));

    mdTypeDef begin_holder_types[FASTPATH_COUNT]{};
    mdTypeDef begin_slow_holder_type = mdTypeDefNil;
    mdTypeDef end_holder_type = mdTypeDefNil;
    mdTypeDef end_void_holder_type = mdTypeDefNil;
    mdTypeDef log_exception_holder_type = mdTypeDefNil;

    for (int arity = 0; arity < FASTPATH_COUNT; arity++)
    {
        std::vector<WSTRING> genericNames{WStr("TMapIntegration"), WStr("TTarget")};
        std::vector<std::vector<COR_SIGNATURE>> parameters;
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 1);
        parameters.push_back(targetArg);
        std::vector<std::vector<COR_SIGNATURE>> delegateArgs{targetArg};
        for (int i = 0; i < arity; i++)
        {
            genericNames.push_back(WStr("TArg") + ToWSTRING(i + 1));
            std::vector<COR_SIGNATURE> arg;
            append_var(arg, static_cast<ULONG>(i + 2), true);
            parameters.push_back(arg);
            std::vector<COR_SIGNATURE> delegateArg;
            append_var(delegateArg, static_cast<ULONG>(i + 2));
            delegateArgs.push_back(delegateArg);
        }

        mdTypeDef holderType;
        const auto holderName = WStr("__OTelCallTargetTrampolineBegin__`") + ToWSTRING(arity + 2);
        IfFailRet(define_holder_type(holderName, genericNames, &holderType));
        begin_holder_types[arity] = holderType;
        mdFieldDef field;
        mdTypeSpec delegateTypeSpec;
        IfFailRet(define_static_delegate_field(holderType, begin_delegate_types[arity], delegateArgs, &field,
                                               &delegateTypeSpec));
        std::vector<mdToken> cctorTypes{var0, var1, delegateTypeSpec};
        IfFailRet(define_holder_cctor(holderType, field, delegateTypeSpec, WStr("CreateBeginDelegate"), cctorTypes,
                                      arity));
        IfFailRet(define_holder_method(holderType, field, delegateTypeSpec, WStr("BeginMethod"), state_sig,
                                       parameters));
    }

    {
        mdTypeDef holderType;
        IfFailRet(define_holder_type(WStr("__OTelCallTargetTrampolineBeginSlow__`2"),
                                     {WStr("TMapIntegration"), WStr("TTarget")}, &holderType));
        begin_slow_holder_type = holderType;
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 1);
        mdFieldDef field;
        mdTypeSpec delegateTypeSpec;
        IfFailRet(define_static_delegate_field(holderType, begin_slow_delegate_type, {targetArg}, &field,
                                               &delegateTypeSpec));
        IfFailRet(define_holder_cctor(holderType, field, delegateTypeSpec, WStr("CreateSlowBeginDelegate"),
                                      {var0, var1, delegateTypeSpec}, -1));
        std::vector<COR_SIGNATURE> objectArrayArg{ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_OBJECT};
        IfFailRet(define_holder_method(holderType, field, delegateTypeSpec, WStr("BeginMethod"), state_sig,
                                       {targetArg, objectArrayArg}));
    }

    {
        mdTypeDef holderType;
        IfFailRet(define_holder_type(WStr("__OTelCallTargetTrampolineEnd__`3"),
                                     {WStr("TMapIntegration"), WStr("TTarget"), WStr("TReturn")}, &holderType));
        end_holder_type = holderType;
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 1);
        std::vector<COR_SIGNATURE> returnArg;
        append_var(returnArg, 2);
        auto returnType = generic_instance_signature(return_generic_type, true, {returnArg});
        mdFieldDef field;
        mdTypeSpec delegateTypeSpec;
        IfFailRet(define_static_delegate_field(holderType, end_delegate_type, {targetArg, returnArg}, &field,
                                               &delegateTypeSpec));
        IfFailRet(define_holder_cctor(holderType, field, delegateTypeSpec, WStr("CreateEndDelegate"),
                                      {var0, var1, var2, delegateTypeSpec}, -1));
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        IfFailRet(define_holder_method(holderType, field, delegateTypeSpec, WStr("EndMethod"), returnType,
                                       {targetArg, returnArg, exception_sig, stateByRef}));
    }

    {
        mdTypeDef holderType;
        IfFailRet(define_holder_type(WStr("__OTelCallTargetTrampolineEndVoid__`2"),
                                     {WStr("TMapIntegration"), WStr("TTarget")}, &holderType));
        end_void_holder_type = holderType;
        std::vector<COR_SIGNATURE> targetArg;
        append_var(targetArg, 1);
        mdFieldDef field;
        mdTypeSpec delegateTypeSpec;
        IfFailRet(define_static_delegate_field(holderType, end_void_delegate_type, {targetArg}, &field,
                                               &delegateTypeSpec));
        IfFailRet(define_holder_cctor(holderType, field, delegateTypeSpec, WStr("CreateEndVoidDelegate"),
                                      {var0, var1, delegateTypeSpec}, -1));
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        IfFailRet(define_holder_method(holderType, field, delegateTypeSpec, WStr("EndMethod"), return_void_sig,
                                       {targetArg, exception_sig, stateByRef}));
    }

    {
        mdTypeDef holderType;
        IfFailRet(define_holder_type(WStr("__OTelCallTargetTrampolineLogException__`2"),
                                     {WStr("TMapIntegration"), WStr("TTarget")}, &holderType));
        log_exception_holder_type = holderType;
        mdFieldDef field;
        mdTypeSpec delegateTypeSpec = log_exception_delegate_type;
        std::vector<COR_SIGNATURE> fieldSig;
        append_type_token(fieldSig, log_exception_delegate_type, false);
        auto finalFieldSig = field_signature(fieldSig);
        IfFailRet(metadata_emit->DefineField(holderType, WStr("_delegate"), fdPrivate | fdStatic, finalFieldSig.data(),
                                             static_cast<ULONG>(finalFieldSig.size()), 0, nullptr, 0, &field));
        IfFailRet(define_holder_cctor(holderType, field, delegateTypeSpec, WStr("CreateLogExceptionDelegate"),
                                      {var0, var1, delegateTypeSpec}, -1));
        IfFailRet(define_holder_method(holderType, field, delegateTypeSpec, WStr("LogException"), void_sig,
                                       {exception_sig}));
    }

    for (int arity = 0; arity < FASTPATH_COUNT; arity++)
    {
        std::vector<WSTRING> genericNames{WStr("TMapIntegration"), WStr("TTarget")};
        std::vector<std::vector<COR_SIGNATURE>> facadeParameters{mvar_signature(1)};
        std::vector<std::vector<COR_SIGNATURE>> holderTypeArguments{mvar_signature(0), mvar_signature(1)};
        std::vector<std::vector<COR_SIGNATURE>> holderParameters{var_signature(1)};
        for (int i = 0; i < arity; i++)
        {
            genericNames.push_back(WStr("TArg") + ToWSTRING(i + 1));
            facadeParameters.push_back(mvar_signature(static_cast<ULONG>(i + 2), true));
            holderTypeArguments.push_back(mvar_signature(static_cast<ULONG>(i + 2)));
            holderParameters.push_back(var_signature(static_cast<ULONG>(i + 2), true));
        }

        IfFailRet(define_trampoline_forwarder(WStr("BeginMethod"), genericNames, state_sig, facadeParameters,
                                              begin_holder_types[arity], holderTypeArguments, WStr("BeginMethod"),
                                              state_sig, holderParameters));
    }

    {
        std::vector<COR_SIGNATURE> objectArrayArg{ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_OBJECT};
        IfFailRet(define_trampoline_forwarder(WStr("BeginMethod"), {WStr("TMapIntegration"), WStr("TTarget")},
                                              state_sig, {mvar_signature(1), objectArrayArg}, begin_slow_holder_type,
                                              {mvar_signature(0), mvar_signature(1)}, WStr("BeginMethod"), state_sig,
                                              {var_signature(1), objectArrayArg}));
    }

    {
        auto returnType = generic_instance_signature(return_generic_type, true, {mvar_signature(2)});
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        IfFailRet(define_trampoline_forwarder(WStr("EndMethod"),
                                              {WStr("TMapIntegration"), WStr("TTarget"), WStr("TReturn")},
                                              returnType,
                                              {mvar_signature(1), mvar_signature(2), exception_sig, stateByRef},
                                              end_holder_type,
                                              {mvar_signature(0), mvar_signature(1), mvar_signature(2)},
                                              WStr("EndMethod"),
                                              generic_instance_signature(return_generic_type, true, {var_signature(2)}),
                                              {var_signature(1), var_signature(2), exception_sig, stateByRef}));
    }

    {
        std::vector<COR_SIGNATURE> stateByRef = state_sig;
        stateByRef.insert(stateByRef.begin(), ELEMENT_TYPE_BYREF);
        IfFailRet(define_trampoline_forwarder(WStr("EndMethod"), {WStr("TMapIntegration"), WStr("TTarget")},
                                              return_void_sig, {mvar_signature(1), exception_sig, stateByRef},
                                              end_void_holder_type, {mvar_signature(0), mvar_signature(1)},
                                              WStr("EndMethod"), return_void_sig,
                                              {var_signature(1), exception_sig, stateByRef}));
    }

    IfFailRet(define_trampoline_forwarder(WStr("LogException"), {WStr("TMapIntegration"), WStr("TTarget")},
                                          void_sig, {exception_sig}, log_exception_holder_type,
                                          {mvar_signature(0), mvar_signature(1)}, WStr("LogException"), void_sig,
                                          {exception_sig}));

    Logger::Info("GenerateCallTargetTrampolineType: CallTarget trampoline v2 types injected into mscorlib.");
    return S_OK;
}


} // namespace trace

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "method_rewriter.h"
#include "cor_profiler.h"
#include "il_rewriter_wrapper.h"
#include "integration.h"
#include "logger.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"
#include "signature_builder.h"
#include "stats.h"
#include "version.h"
#include "environment_variables_util.h"

namespace trace
{

namespace
{
const WSTRING calltarget_trampoline_type_name = WStr("__OTelCallTargetTrampoline__");

struct CallTargetTrampolineTokens
{
    mdAssemblyRef corlibRef = mdAssemblyRefNil;
    mdToken objectType = mdTokenNil;
    mdToken exceptionType = mdTokenNil;
    mdToken stringType = mdTokenNil;
    mdToken runtimeMethodHandleType = mdTokenNil;
    mdToken runtimeTypeHandleType = mdTokenNil;
    mdToken trampolineType = mdTokenNil;
    mdMemberRef beginMethod = mdMemberRefNil;
    mdMemberRef endMethod = mdMemberRefNil;
    mdMemberRef endVoidMethod = mdMemberRefNil;
    mdMemberRef logExceptionMethod = mdMemberRefNil;
    mdString integrationAssemblyString = mdStringNil;
    mdString integrationTypeString = mdStringNil;
};

mdAssemblyRef FindExistingCorLibRef(ModuleMetadata& moduleMetadata)
{
    if (moduleMetadata.assemblyName == mscorlib_assemblyName || moduleMetadata.assemblyName == system_private_corelib_assemblyName)
    {
        return mdTokenNil;
    }

    for (mdAssemblyRef assemblyRef : EnumAssemblyRefs(moduleMetadata.assembly_import))
    {
        auto assemblyMetadata = GetReferencedAssemblyMetadata(moduleMetadata.assembly_import, assemblyRef);
        if (assemblyMetadata.name == mscorlib_assemblyName || assemblyMetadata.name == system_private_corelib_assemblyName)
        {
            return assemblyRef;
        }
    }

    return mdAssemblyRefNil;
}

HRESULT BuildCallTargetTrampolineTokens(ModuleMetadata&        moduleMetadata,
                                        IntegrationDefinition* integrationDefinition,
                                        CallTargetTrampolineTokens& tokens)
{
    tokens.corlibRef = FindExistingCorLibRef(moduleMetadata);
    if (tokens.corlibRef == mdAssemblyRefNil &&
        moduleMetadata.assemblyName != mscorlib_assemblyName &&
        moduleMetadata.assemblyName != system_private_corelib_assemblyName)
    {
        Logger::Warn("BuildCallTargetTrampolineTokens: skipping module without existing corlib AssemblyRef: ",
                     moduleMetadata.assemblyName);
        return E_FAIL;
    }

    MemberResolver resolver(moduleMetadata.metadata_import, moduleMetadata.metadata_emit);
    HRESULT        hr = S_OK;

    auto get_type = [&](LPCWSTR name, mdToken* token) -> HRESULT {
        hr = resolver.GetTypeRefOrDefByName(tokens.corlibRef, name, token);
        if (FAILED(hr))
        {
            Logger::Warn("BuildCallTargetTrampolineTokens: failed to resolve type ", WSTRING(name));
        }
        return hr;
    };

    IfFailRet(get_type(SystemObject, &tokens.objectType));
    IfFailRet(get_type(SystemException, &tokens.exceptionType));
    IfFailRet(get_type(SystemString, &tokens.stringType));
    IfFailRet(get_type(RuntimeMethodHandleTypeName, &tokens.runtimeMethodHandleType));
    IfFailRet(get_type(RuntimeTypeHandleTypeName, &tokens.runtimeTypeHandleType));
    IfFailRet(get_type(calltarget_trampoline_type_name.c_str(), &tokens.trampolineType));

    SignatureBuilder::Type objectType{SignatureBuilder::BuiltIn::Object};
    SignatureBuilder::Class objectClass{tokens.objectType};
    SignatureBuilder::Class exceptionClass{tokens.exceptionType};
    SignatureBuilder::Class stringClass{tokens.stringType};
    SignatureBuilder::ValueType runtimeMethodHandleValue{tokens.runtimeMethodHandleType};
    SignatureBuilder::ValueType runtimeTypeHandleValue{tokens.runtimeTypeHandleType};
    SignatureBuilder::Array objectArray{objectType};

    {
        SignatureBuilder::StaticMethod signature{objectType,
                                                 {runtimeMethodHandleValue, runtimeTypeHandleValue, stringClass,
                                                  stringClass, objectClass, objectArray}};
        hr = resolver.GetMemberRefOrDef(tokens.trampolineType, WStr("Begin"), signature.Head(), signature.Size(),
                                        &tokens.beginMethod);
        if (FAILED(hr))
        {
            Logger::Warn("BuildCallTargetTrampolineTokens: failed to define Begin member ref");
            return hr;
        }
    }

    {
        SignatureBuilder signature;
        signature.PushRawByte(IMAGE_CEE_CS_CALLCONV_GENERIC)
            .PushCompressedData(1)
            .PushCompressedData(8)
            .PushRawByte(ELEMENT_TYPE_MVAR)
            .PushCompressedData(0)
            .Push(runtimeMethodHandleValue)
            .Push(runtimeTypeHandleValue)
            .Push(stringClass)
            .Push(stringClass)
            .Push(objectClass)
            .PushRawByte(ELEMENT_TYPE_MVAR)
            .PushCompressedData(0)
            .Push(exceptionClass)
            .Push(objectClass);
        hr = resolver.GetMemberRefOrDef(tokens.trampolineType, WStr("End"), signature.Head(), signature.Size(),
                                        &tokens.endMethod);
        if (FAILED(hr))
        {
            Logger::Warn("BuildCallTargetTrampolineTokens: failed to define End member ref");
            return hr;
        }
    }

    {
        SignatureBuilder::StaticMethod signature{SignatureBuilder::Type{SignatureBuilder::BuiltIn::Void},
                                                 {runtimeMethodHandleValue, runtimeTypeHandleValue, stringClass,
                                                  stringClass, objectClass, exceptionClass, objectClass}};
        hr = resolver.GetMemberRefOrDef(tokens.trampolineType, WStr("EndVoid"), signature.Head(), signature.Size(),
                                        &tokens.endVoidMethod);
        if (FAILED(hr))
        {
            Logger::Warn("BuildCallTargetTrampolineTokens: failed to define EndVoid member ref");
            return hr;
        }
    }

    {
        SignatureBuilder::StaticMethod signature{SignatureBuilder::Type{SignatureBuilder::BuiltIn::Void},
                                                 {runtimeMethodHandleValue, runtimeTypeHandleValue, stringClass,
                                                  stringClass, exceptionClass}};
        hr = resolver.GetMemberRefOrDef(tokens.trampolineType, WStr("LogException"), signature.Head(),
                                        signature.Size(), &tokens.logExceptionMethod);
        if (FAILED(hr))
        {
            Logger::Warn("BuildCallTargetTrampolineTokens: failed to define LogException member ref");
            return hr;
        }
    }

    const WSTRING integrationAssemblyName = integrationDefinition->integration_type.assembly.str();
    hr = moduleMetadata.metadata_emit->DefineUserString(integrationAssemblyName.c_str(),
                                                        static_cast<ULONG>(integrationAssemblyName.size()),
                                                        &tokens.integrationAssemblyString);
    if (FAILED(hr))
    {
        Logger::Warn("BuildCallTargetTrampolineTokens: failed to define integration assembly string");
        return hr;
    }

    const WSTRING integrationTypeName = integrationDefinition->integration_type.name;
    hr = moduleMetadata.metadata_emit->DefineUserString(integrationTypeName.c_str(),
                                                        static_cast<ULONG>(integrationTypeName.size()),
                                                        &tokens.integrationTypeString);
    if (FAILED(hr))
    {
        Logger::Warn("BuildCallTargetTrampolineTokens: failed to define integration type string");
        return hr;
    }

    return S_OK;
}

void AppendCompressedData(std::vector<COR_SIGNATURE>& signature, ULONG value)
{
    COR_SIGNATURE buffer[sizeof(ULONG)];
    ULONG         size = CorSigCompressData(value, buffer);
    signature.insert(signature.end(), buffer, buffer + size);
}

void AppendCompressedToken(std::vector<COR_SIGNATURE>& signature, mdToken token)
{
    COR_SIGNATURE buffer[sizeof(mdToken)];
    ULONG         size = CorSigCompressToken(token, buffer);
    signature.insert(signature.end(), buffer, buffer + size);
}

// Extends the target method local signature and emits the trampoline prologue:
//
// TReturn otelReturnValue = default; // only for non-void methods
// Exception otelException = null;
// object otelState = null;
HRESULT ModifyLocalSigAndInitializeForTrampoline(ILRewriterWrapper&            reWriterWrapper,
                                                 ModuleMetadata&               moduleMetadata,
                                                 FunctionInfo*                 functionInfo,
                                                 CallTargetTrampolineTokens&   tokens,
                                                 ULONG*                        exceptionIndex,
                                                 ULONG*                        stateIndex,
                                                 ULONG*                        returnValueIndex,
                                                 ILInstr**                     firstInstruction)
{
    ILRewriter* rewriter = reWriterWrapper.GetILRewriter();
    HRESULT     hr       = S_OK;

    PCCOR_SIGNATURE originalSignature     = nullptr;
    ULONG           originalSignatureSize = 0;
    mdToken         localVarSig           = rewriter->GetTkLocalVarSig();
    ULONG           oldLocalsCount        = 0;
    ULONG           oldLocalsLen          = 0;

    if (localVarSig != mdTokenNil)
    {
        IfFailRet(moduleMetadata.metadata_import->GetSigFromToken(localVarSig, &originalSignature,
                                                                  &originalSignatureSize));
        oldLocalsLen = CorSigUncompressData(originalSignature + 1, &oldLocalsCount);
    }

    auto returnFunctionMethod = functionInfo->method_signature.GetReturnValue();
    const auto [retFuncElementType, retTypeFlags] = returnFunctionMethod.GetElementTypeAndFlags();
    const bool isVoid = (retTypeFlags & TypeFlagVoid) > 0;

    ULONG newLocalsToAdd = isVoid ? 2 : 3;
    ULONG newLocalsCount = oldLocalsCount + newLocalsToAdd;

    std::vector<COR_SIGNATURE> newSignature;
    newSignature.reserve(originalSignatureSize + 32);
    newSignature.push_back(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);
    AppendCompressedData(newSignature, newLocalsCount);

    if (originalSignatureSize > 0)
    {
        const auto copyStart  = originalSignature + 1 + oldLocalsLen;
        const auto copyLength = originalSignatureSize - 1 - oldLocalsLen;
        newSignature.insert(newSignature.end(), copyStart, copyStart + copyLength);
    }

    PCCOR_SIGNATURE returnSignatureType     = nullptr;
    ULONG           returnSignatureTypeSize = 0;
    if (!isVoid)
    {
        returnSignatureTypeSize = returnFunctionMethod.GetSignature(returnSignatureType);
        newSignature.insert(newSignature.end(), returnSignatureType, returnSignatureType + returnSignatureTypeSize);
    }

    newSignature.push_back(ELEMENT_TYPE_CLASS);
    AppendCompressedToken(newSignature, tokens.exceptionType);

    newSignature.push_back(ELEMENT_TYPE_OBJECT);

    mdToken newLocalVarSig;
    hr = moduleMetadata.metadata_emit->GetTokenFromSig(newSignature.data(), static_cast<ULONG>(newSignature.size()),
                                                       &newLocalVarSig);
    if (FAILED(hr))
    {
        Logger::Warn("ModifyLocalSigAndInitializeForTrampoline: failed to create local signature");
        return hr;
    }

    rewriter->SetTkLocalVarSig(newLocalVarSig);

    if (!isVoid)
    {
        *returnValueIndex = newLocalsCount - 3;
    }
    else
    {
        *returnValueIndex = static_cast<ULONG>(ULONG_MAX);
    }

    *exceptionIndex = newLocalsCount - 2;
    *stateIndex     = newLocalsCount - 1;

    if (!isVoid)
    {
        if ((retTypeFlags & TypeFlagBoxedType) > 0)
        {
            auto    metadataEmit    = moduleMetadata.metadata_emit;
            mdToken returnTypeToken = returnFunctionMethod.GetTypeTok(metadataEmit, tokens.corlibRef);
            if (returnTypeToken == mdTokenNil)
            {
                Logger::Warn("ModifyLocalSigAndInitializeForTrampoline: failed to resolve return type token");
                return E_FAIL;
            }

            *firstInstruction = reWriterWrapper.LoadLocalAddress(*returnValueIndex);
            reWriterWrapper.InitObj(returnTypeToken);
        }
        else
        {
            *firstInstruction = reWriterWrapper.LoadNull();
            reWriterWrapper.StLocal(*returnValueIndex);
        }
    }

    ILInstr* loadExceptionNull = reWriterWrapper.LoadNull();
    if (isVoid)
    {
        *firstInstruction = loadExceptionNull;
    }
    reWriterWrapper.StLocal(*exceptionIndex);
    reWriterWrapper.LoadNull();
    reWriterWrapper.StLocal(*stateIndex);
    return S_OK;
}

// Emits the instance argument for the public trampoline methods:
//
// object instance = isStatic ? null : (object)this;
//
// Value-type instances are loaded from the by-ref this pointer, copied, and boxed.
bool LoadInstanceAsObject(ILRewriterWrapper& reWriterWrapper, FunctionInfo* caller, bool isStatic, ILInstr** firstInstruction)
{
    if (isStatic)
    {
        if (caller->type.valueType)
        {
            Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Static methods in a ValueType cannot be "
                         "instrumented. ");
            return false;
        }

        *firstInstruction = reWriterWrapper.LoadNull();
        return true;
    }

    *firstInstruction = reWriterWrapper.LoadArgument(0);
    if (caller->type.valueType)
    {
        mdToken valueTypeToken = mdTokenNil;
        if (caller->type.type_spec != mdTypeSpecNil)
        {
            valueTypeToken = caller->type.type_spec;
        }
        else if (!caller->type.isGeneric)
        {
            valueTypeToken = caller->type.id;
        }
        else
        {
            Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Generic struct instrumentation is not "
                         "supported. ");
            return false;
        }

        reWriterWrapper.LoadObj(valueTypeToken);
        reWriterWrapper.Box(valueTypeToken);
    }

    return true;
}

// Emits the identity arguments that let mscorlib resolve the target/integration pair:
//
// RuntimeMethodHandle targetMethod = methodof(CurrentMethod).MethodHandle;
// RuntimeTypeHandle targetType = typeof(CurrentRuntimeType).TypeHandle;
// string integrationAssembly = "...";
// string integrationType = "...";
ILInstr* LoadTrampolineIdentity(ILRewriterWrapper& reWriterWrapper,
                                FunctionInfo* caller,
                                const CallTargetTrampolineTokens& tokens)
{
    ILInstr* firstInstruction = reWriterWrapper.LoadToken(caller->id);

    mdToken targetTypeToken = caller->type.type_spec;
    if (targetTypeToken == mdTypeSpecNil)
    {
        const TypeInfo* currentType = &caller->type;
        while (!currentType->isGeneric)
        {
            if (currentType->parent_type == nullptr)
            {
                targetTypeToken = currentType->id;
                break;
            }

            currentType = currentType->parent_type.get();
        }

        if (targetTypeToken == mdTypeSpecNil)
        {
            targetTypeToken = tokens.objectType;
        }
    }

    reWriterWrapper.LoadToken(targetTypeToken);
    reWriterWrapper.LoadString(tokens.integrationAssemblyString);
    reWriterWrapper.LoadString(tokens.integrationTypeString);
    return firstInstruction;
}

// Emits the generic non-void End call after the caller has already pushed the shared arguments:
//
// otelReturnValue = __OTelCallTargetTrampoline__.End<TReturn>(
//     targetMethod, targetType, integrationAssembly, integrationType,
//     instance, otelReturnValue, otelException, otelState);
HRESULT WriteTrampolineEndMethod(ILRewriterWrapper&          reWriterWrapper,
                                 ModuleMetadata&             moduleMetadata,
                                 CallTargetTrampolineTokens& tokens,
                                 TypeSignature*              returnArgument,
                                 ILInstr**                   instruction)
{
    PCCOR_SIGNATURE returnSignature = nullptr;
    ULONG           returnSignatureLength = returnArgument->GetSignature(returnSignature);

    SignatureBuilder methodSpecSignature;
    methodSpecSignature.PushRawByte(IMAGE_CEE_CS_CALLCONV_GENERICINST).PushCompressedData(1).PushRawBytes(
        returnSignature, returnSignature + returnSignatureLength);

    mdMethodSpec endMethodSpec = mdMethodSpecNil;
    auto hr = moduleMetadata.metadata_emit->DefineMethodSpec(tokens.endMethod, methodSpecSignature.Head(),
                                                             methodSpecSignature.Size(), &endMethodSpec);
    if (FAILED(hr))
    {
        Logger::Warn("WriteTrampolineEndMethod: failed to define End<TReturn> method spec");
        return hr;
    }

    *instruction = reWriterWrapper.CallMember(endMethodSpec, false);
    return S_OK;
}

// clang-format off
// Rewrites the target method body into this conceptual C# shape. "OriginalBody" means the original
// IL instructions are kept in place; their ret instructions are replaced with leave instructions.
//
// TReturn TargetMethod(args)
// {
//     TReturn otelReturnValue = default; // omitted for void
//     Exception otelException = null;
//     object otelState = null;
//
//     try
//     {
//         try
//         {
//             otelState = __OTelCallTargetTrampoline__.Begin(
//                 methodof(TargetMethod).MethodHandle,
//                 typeof(CurrentRuntimeType).TypeHandle,
//                 integrationAssembly,
//                 integrationType,
//                 instance,
//                 new object[] { boxedArgs });
//         }
//         catch (Exception ex)
//         {
//             __OTelCallTargetTrampoline__.LogException(
//                 methodof(TargetMethod).MethodHandle,
//                 typeof(CurrentRuntimeType).TypeHandle,
//                 integrationAssembly,
//                 integrationType,
//                 ex);
//             otelException = null;
//         }
//
//         otelReturnValue = OriginalBody(args); // for non-void; void just runs OriginalBody(args)
//     }
//     catch (Exception ex)
//     {
//         otelException = ex;
//         throw;
//     }
//     finally
//     {
//         try
//         {
//             if (voidMethod)
//             {
//                 __OTelCallTargetTrampoline__.EndVoid(
//                     methodof(TargetMethod).MethodHandle,
//                     typeof(CurrentRuntimeType).TypeHandle,
//                     integrationAssembly,
//                     integrationType,
//                     instance,
//                     otelException,
//                     otelState);
//             }
//             else
//             {
//                 otelReturnValue = __OTelCallTargetTrampoline__.End<TReturn>(
//                     methodof(TargetMethod).MethodHandle,
//                     typeof(CurrentRuntimeType).TypeHandle,
//                     integrationAssembly,
//                     integrationType,
//                     instance,
//                     otelReturnValue,
//                     otelException,
//                     otelState);
//             }
//         }
//         catch (Exception ex)
//         {
//             otelException = ex;
//             __OTelCallTargetTrampoline__.LogException(
//                 methodof(TargetMethod).MethodHandle,
//                 typeof(CurrentRuntimeType).TypeHandle,
//                 integrationAssembly,
//                 integrationType,
//                 ex);
//         }
//     }
//
//     return otelReturnValue; // omitted for void
// }
// clang-format on
HRESULT RewriteWithCallTargetTrampoline(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler)
{
    auto tracerMethodHandler = static_cast<TracerRejitHandlerModuleMethod*>(methodHandler);

    auto corProfiler = trace::profiler;

    ModuleID               module_id              = moduleHandler->GetModuleId();
    ModuleMetadata&        module_metadata        = *moduleHandler->GetModuleMetadata();
    FunctionInfo*          caller                 = methodHandler->GetFunctionInfo();
    mdToken                function_token         = caller->id;
    TypeSignature          retFuncArg             = caller->method_signature.GetReturnValue();
    IntegrationDefinition* integration_definition = tracerMethodHandler->GetIntegrationDefinition();
    bool                   is_integration_method =
        integration_definition->target_method.type.assembly.name != tracemethodintegration_assemblyname;
    const auto [retFuncElementType, retTypeFlags] = retFuncArg.GetElementTypeAndFlags();
    bool isVoid                                   = (retTypeFlags & TypeFlagVoid) > 0;
    bool isStatic = !(caller->method_signature.CallingConvention() & IMAGE_CEE_CS_CALLCONV_HASTHIS);
    std::vector<trace::TypeSignature> methodArguments = caller->method_signature.GetMethodArguments();
    int                               numArgs    = caller->method_signature.NumberOfArguments();
    auto                              metaEmit   = module_metadata.metadata_emit;

    if (!is_integration_method)
    {
        Logger::Warn("*** CallTarget_Trampoline_RewriterCallback() skipping trace-method instrumentation for ",
                     caller->type.name, ".", caller->name, "().");
        return S_FALSE;
    }

    if ((retTypeFlags & TypeFlagByRef) > 0)
    {
        Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Methods with by-ref returns cannot be "
                     "instrumented. ");
        return S_FALSE;
    }

    for (int i = 0; i < numArgs; i++)
    {
        const auto [elementType, argTypeFlags] = methodArguments[i].GetElementTypeAndFlags();
        if ((argTypeFlags & TypeFlagByRef) > 0)
        {
            Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Methods with ref parameters cannot be "
                         "instrumented. ");
            return S_FALSE;
        }
    }

    if (!corProfiler->IsProfilerAssemblyLoadedIntoAppDomain(module_metadata.app_domain_id))
    {
        Logger::Warn("*** CallTarget_Trampoline_RewriterCallback() skipping method: Method replacement found but the "
                     "managed profiler has not yet been loaded into AppDomain with id=",
                     module_metadata.app_domain_id, " token=", function_token, " caller_name=", caller->type.name, ".",
                     caller->name, "()");
        return S_FALSE;
    }

    CallTargetTrampolineTokens trampolineTokens;
    auto hr = BuildCallTargetTrampolineTokens(module_metadata, integration_definition, trampolineTokens);
    if (FAILED(hr))
    {
        return S_FALSE;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("*** CallTarget_Trampoline_RewriterCallback() Start: ", caller->type.name, ".", caller->name,
                      "() [IsVoid=", isVoid, ", IsStatic=", isStatic,
                      ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs,
                      "]");
    }

    ILRewriter rewriter(corProfiler->GetCorProfilerInfo(), methodHandler->GetFunctionControl(), module_id,
                        function_token);
    hr = rewriter.Import();
    if (FAILED(hr))
    {
        Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Call to ILRewriter.Import() failed for ",
                     module_id, " ", function_token);
        return S_FALSE;
    }

    std::string original_code;
    if (IsDumpILRewriteEnabled())
    {
        original_code = corProfiler->GetILCodes("*** CallTarget_Trampoline_RewriterCallback(): Original Code: ",
                                                &rewriter, *caller, module_metadata.metadata_import);
    }

    ILRewriterWrapper reWriterWrapper(&rewriter);
    reWriterWrapper.SetILPosition(rewriter.GetILList()->m_pNext);

    ULONG    exceptionIndex   = static_cast<ULONG>(ULONG_MAX);
    ULONG    stateIndex       = static_cast<ULONG>(ULONG_MAX);
    ULONG    returnValueIndex = static_cast<ULONG>(ULONG_MAX);
    ILInstr* firstInstruction = nullptr;

    hr = ModifyLocalSigAndInitializeForTrampoline(reWriterWrapper, module_metadata, caller, trampolineTokens,
                                                  &exceptionIndex, &stateIndex, &returnValueIndex, &firstInstruction);
    if (FAILED(hr))
    {
        return S_FALSE;
    }

    LoadTrampolineIdentity(reWriterWrapper, caller, trampolineTokens);

    ILInstr* instanceLoadInstruction = nullptr;
    if (!LoadInstanceAsObject(reWriterWrapper, caller, isStatic, &instanceLoadInstruction))
    {
        return S_FALSE;
    }

    reWriterWrapper.CreateArray(trampolineTokens.objectType, numArgs);
    for (int i = 0; i < numArgs; i++)
    {
        reWriterWrapper.BeginLoadValueIntoArray(i);
        reWriterWrapper.LoadArgument(i + (isStatic ? 0 : 1));
        const auto [elementType, argTypeFlags] = methodArguments[i].GetElementTypeAndFlags();
        if ((argTypeFlags & TypeFlagBoxedType) > 0)
        {
            const auto& tok = methodArguments[i].GetTypeTok(metaEmit, trampolineTokens.corlibRef);
            if (tok == mdTokenNil)
            {
                return S_FALSE;
            }
            reWriterWrapper.Box(tok);
        }
        reWriterWrapper.EndLoadValueIntoArray();
    }

    reWriterWrapper.CallMember(trampolineTokens.beginMethod, false);
    reWriterWrapper.StLocal(stateIndex);
    ILInstr* pStateLeaveToBeginOriginalMethodInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    ILInstr* beginMethodCatchFirstInstr = reWriterWrapper.StLocal(exceptionIndex);
    LoadTrampolineIdentity(reWriterWrapper, caller, trampolineTokens);
    reWriterWrapper.LoadLocal(exceptionIndex);
    reWriterWrapper.CallMember(trampolineTokens.logExceptionMethod, false);
    reWriterWrapper.LoadNull();
    reWriterWrapper.StLocal(exceptionIndex);
    ILInstr* beginMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    EHClause beginMethodExClause{};
    beginMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    beginMethodExClause.m_pTryBegin     = firstInstruction;
    beginMethodExClause.m_pTryEnd       = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerBegin = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerEnd   = beginMethodCatchLeaveInstr;
    beginMethodExClause.m_ClassToken    = trampolineTokens.exceptionType;

    ILInstr* beginOriginalMethodInstr                = reWriterWrapper.GetCurrentILInstr();
    pStateLeaveToBeginOriginalMethodInstr->m_pTarget = beginOriginalMethodInstr;
    beginMethodCatchLeaveInstr->m_pTarget            = beginOriginalMethodInstr;

    ILInstr* methodReturnInstr  = rewriter.NewILInstr();
    methodReturnInstr->m_opcode = CEE_RET;
    rewriter.InsertAfter(rewriter.GetILList()->m_pPrev, methodReturnInstr);
    reWriterWrapper.SetILPosition(methodReturnInstr);

    ILInstr* startExceptionCatch = reWriterWrapper.StLocal(exceptionIndex);
    reWriterWrapper.SetILPosition(methodReturnInstr);
    ILInstr* rethrowInstr = reWriterWrapper.Rethrow();

    ILInstr* endMethodTryStartInstr = LoadTrampolineIdentity(reWriterWrapper, caller, trampolineTokens);
    ILInstr* endMethodInstanceLoadInstr = nullptr;
    if (!LoadInstanceAsObject(reWriterWrapper, caller, isStatic, &endMethodInstanceLoadInstr))
    {
        return S_FALSE;
    }

    if (!isVoid)
    {
        reWriterWrapper.LoadLocal(returnValueIndex);
    }

    reWriterWrapper.LoadLocal(exceptionIndex);
    reWriterWrapper.LoadLocal(stateIndex);

    ILInstr* endMethodCallInstr;
    if (isVoid)
    {
        endMethodCallInstr = reWriterWrapper.CallMember(trampolineTokens.endVoidMethod, false);
    }
    else
    {
        hr = WriteTrampolineEndMethod(reWriterWrapper, module_metadata, trampolineTokens, &retFuncArg,
                                      &endMethodCallInstr);
        if (FAILED(hr))
        {
            return S_FALSE;
        }
        reWriterWrapper.StLocal(returnValueIndex);
    }

    ILInstr* endMethodTryLeave = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    ILInstr* endMethodCatchFirstInstr = reWriterWrapper.StLocal(exceptionIndex);
    LoadTrampolineIdentity(reWriterWrapper, caller, trampolineTokens);
    reWriterWrapper.LoadLocal(exceptionIndex);
    reWriterWrapper.CallMember(trampolineTokens.logExceptionMethod, false);
    ILInstr* endMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    EHClause endMethodExClause{};
    endMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    endMethodExClause.m_pTryBegin     = endMethodTryStartInstr;
    endMethodExClause.m_pTryEnd       = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerBegin = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerEnd   = endMethodCatchLeaveInstr;
    endMethodExClause.m_ClassToken    = trampolineTokens.exceptionType;

    ILInstr* endFinallyInstr            = reWriterWrapper.EndFinally();
    endMethodTryLeave->m_pTarget        = endFinallyInstr;
    endMethodCatchLeaveInstr->m_pTarget = endFinallyInstr;

    if (!isVoid)
    {
        reWriterWrapper.LoadLocal(returnValueIndex);
    }

    for (ILInstr* pInstr = rewriter.GetILList()->m_pNext; pInstr != rewriter.GetILList(); pInstr = pInstr->m_pNext)
    {
        switch (pInstr->m_opcode)
        {
            case CEE_RET:
            {
                if (pInstr != methodReturnInstr)
                {
                    if (!isVoid)
                    {
                        reWriterWrapper.SetILPosition(pInstr);
                        reWriterWrapper.StLocal(returnValueIndex);
                    }
                    pInstr->m_opcode  = CEE_LEAVE_S;
                    pInstr->m_pTarget = endFinallyInstr->m_pNext;
                }
                break;
            }
            default:
                break;
        }
    }

    EHClause exClause{};
    exClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    exClause.m_pTryBegin     = firstInstruction;
    exClause.m_pTryEnd       = startExceptionCatch;
    exClause.m_pHandlerBegin = startExceptionCatch;
    exClause.m_pHandlerEnd   = rethrowInstr;
    exClause.m_ClassToken    = trampolineTokens.exceptionType;

    EHClause finallyClause{};
    finallyClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_FINALLY;
    finallyClause.m_pTryBegin     = firstInstruction;
    finallyClause.m_pTryEnd       = rethrowInstr->m_pNext;
    finallyClause.m_pHandlerBegin = rethrowInstr->m_pNext;
    finallyClause.m_pHandlerEnd   = endFinallyInstr;

    auto ehCount      = rewriter.GetEHCount();
    auto ehPointer    = rewriter.GetEHPointer();
    auto newEHClauses = new EHClause[ehCount + 4];
    for (unsigned i = 0; i < ehCount; i++)
    {
        newEHClauses[i] = ehPointer[i];
    }

    ehCount += 4;
    newEHClauses[ehCount - 4] = beginMethodExClause;
    newEHClauses[ehCount - 3] = endMethodExClause;
    newEHClauses[ehCount - 2] = exClause;
    newEHClauses[ehCount - 1] = finallyClause;
    rewriter.SetEHClause(newEHClauses, ehCount);

    if (IsDumpILRewriteEnabled())
    {
        Logger::Info(original_code);
        Logger::Info(corProfiler->GetILCodes("*** CallTarget_Trampoline_RewriterCallback(): Modified Code: ",
                                             &rewriter, *caller, module_metadata.metadata_import));
    }

    hr = rewriter.Export();

    if (FAILED(hr))
    {
        Logger::Warn("*** CallTarget_Trampoline_RewriterCallback(): Call to ILRewriter.Export() failed for ModuleID=",
                     module_id, " ", function_token);
        return S_FALSE;
    }

    Logger::Info("*** CallTarget_Trampoline_RewriterCallback() Finished: ", caller->type.name, ".", caller->name,
                 "() [IsVoid=", isVoid, ", IsStatic=", isStatic,
                 ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs, "]");
    return S_OK;
}
} // namespace

/// <summary>
/// Rewrite the target method body with the calltarget implementation. (This is function is triggered by the ReJIT
/// handler) Resulting code structure:
///
/// - Add locals for TReturn (if non-void method), CallTargetState, CallTargetReturn/CallTargetReturn<TReturn>,
/// Exception
/// - Initialize locals
///
/// try
/// {
///   try
///   {
///     try
///     {
///       - Invoke BeginMethod with object instance (or null if static method) and original method arguments
///       - Store result into CallTargetState local
///     }
///     catch
///     {
///       - Invoke LogException(Exception)
///     }
///
///     - Execute original method instructions
///       * All RET instructions are replaced with a LEAVE_S. If non-void method, the value on the stack is first stored
///       in the TReturn local.
///   }
///   catch (Exception)
///   {
///     - Store exception into Exception local
///     - throw
///   }
/// }
/// finally
/// {
///   try
///   {
///     - Invoke EndMethod with object instance (or null if static method), TReturn local (if non-void method),
///     CallTargetState local, and Exception local
///     - Store result into CallTargetReturn/CallTargetReturn<TReturn> local
///     - If non-void method, store CallTargetReturn<TReturn>.GetReturnValue() into TReturn local
///   }
///   catch
///   {
///     - Invoke LogException(Exception)
///   }
/// }
///
/// - If non-void method, load TReturn local
/// - RET
/// </summary>
/// <param name="moduleHandler">Module ReJIT handler representation</param>
/// <param name="methodHandler">Method ReJIT handler representation</param>
/// <returns>Result of the rewriting</returns>
HRESULT TracerMethodRewriter::Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler)
{
    if (methodHandler == nullptr)
    {
        Logger::Error("TracerMethodRewriter::Rewrite: methodHandler is null. "
                      "MethodDef: ",
                      methodHandler->GetMethodDef());

        return S_FALSE;
    }

    auto tracerMethodHandler = static_cast<TracerRejitHandlerModuleMethod*>(methodHandler);

    if (tracerMethodHandler->GetIntegrationDefinition() == nullptr)
    {
        Logger::Warn("TracerMethodRewriter::Rewrite: IntegrationDefinition is missing for "
                     "MethodDef: ",
                     methodHandler->GetMethodDef());

        return S_FALSE;
    }

    auto _ = trace::Stats::Instance()->CallTargetRewriterCallbackMeasure();

    auto corProfiler = trace::profiler;

    ModuleID               module_id              = moduleHandler->GetModuleId();
    ModuleMetadata&        module_metadata        = *moduleHandler->GetModuleMetadata();
    FunctionInfo*          caller                 = methodHandler->GetFunctionInfo();
    TracerTokens*          tracerTokens           = module_metadata.GetTracerTokens();
    mdToken                function_token         = caller->id;
    TypeSignature          retFuncArg             = caller->method_signature.GetReturnValue();
    IntegrationDefinition* integration_definition = tracerMethodHandler->GetIntegrationDefinition();
    bool                   is_integration_method =
        integration_definition->target_method.type.assembly.name != tracemethodintegration_assemblyname;
    bool ignoreByRefInstrumentation               = !is_integration_method;
    const auto [retFuncElementType, retTypeFlags] = retFuncArg.GetElementTypeAndFlags();
    bool isVoid                                   = (retTypeFlags & TypeFlagVoid) > 0;
    bool isStatic = !(caller->method_signature.CallingConvention() & IMAGE_CEE_CS_CALLCONV_HASTHIS);
    std::vector<trace::TypeSignature> methodArguments = caller->method_signature.GetMethodArguments();
    std::vector<trace::TypeSignature> traceAnnotationArguments;
    COR_SIGNATURE                     runtimeMethodHandleBuffer[10];
    COR_SIGNATURE                     runtimeTypeHandleBuffer[10];
    int                               numArgs    = caller->method_signature.NumberOfArguments();
    auto                              metaEmit   = module_metadata.metadata_emit;
    auto                              metaImport = module_metadata.metadata_import;

    if (corProfiler->IsCallTargetTrampolineEnabled())
    {
        return RewriteWithCallTargetTrampoline(moduleHandler, methodHandler);
    }

    // *** Get reference to the integration type
    mdTypeRef integration_type_ref = mdTypeRefNil;
    if (!corProfiler->GetIntegrationTypeRef(module_metadata, module_id, *integration_definition, integration_type_ref))
    {
        Logger::Warn("*** CallTarget_RewriterCallback() skipping method: Integration Type Ref cannot be found for ",
                     " token=", function_token, " caller_name=", caller->type.name, ".", caller->name, "()");
        return S_FALSE;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("*** CallTarget_RewriterCallback() Start: ", caller->type.name, ".", caller->name,
                      "() [IsVoid=", isVoid, ", IsStatic=", isStatic,
                      ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs,
                      "]");
    }

    // First we check if the managed profiler has not been loaded yet
    if (!corProfiler->ProfilerAssemblyIsLoadedIntoAppDomain(module_metadata.app_domain_id))
    {
        Logger::Warn(
            "*** CallTarget_RewriterCallback() skipping method: Method replacement found but the managed profiler has "
            "not yet been loaded into AppDomain with id=",
            module_metadata.app_domain_id, " token=", function_token, " caller_name=", caller->type.name, ".",
            caller->name, "()");
        return S_FALSE;
    }

    // *** Create rewriter
    ILRewriter rewriter(corProfiler->info_, methodHandler->GetFunctionControl(), module_id, function_token);
    bool       modified = false;
    auto       hr       = rewriter.Import();
    if (FAILED(hr))
    {
        Logger::Warn("*** CallTarget_RewriterCallback(): Call to ILRewriter.Import() failed for ", module_id, " ",
                     function_token);
        return S_FALSE;
    }

    // *** Store the original il code text if the dump_il option is enabled.
    std::string original_code;
    if (IsDumpILRewriteEnabled())
    {
        original_code = corProfiler->GetILCodes("*** CallTarget_RewriterCallback(): Original Code: ", &rewriter,
                                                *caller, module_metadata.metadata_import);
    }

    // *** Create the rewriter wrapper helper
    ILRewriterWrapper reWriterWrapper(&rewriter);
    reWriterWrapper.SetILPosition(rewriter.GetILList()->m_pNext);

    // *** Modify the Local Var Signature of the method and initialize the new local vars
    ULONG    callTargetStateIndex  = static_cast<ULONG>(ULONG_MAX);
    ULONG    exceptionIndex        = static_cast<ULONG>(ULONG_MAX);
    ULONG    callTargetReturnIndex = static_cast<ULONG>(ULONG_MAX);
    ULONG    returnValueIndex      = static_cast<ULONG>(ULONG_MAX);
    mdToken  callTargetStateToken  = mdTokenNil;
    mdToken  exceptionToken        = mdTokenNil;
    mdToken  callTargetReturnToken = mdTokenNil;
    ILInstr* firstInstruction      = nullptr;
    tracerTokens->ModifyLocalSigAndInitialize(&reWriterWrapper, caller, &callTargetStateIndex, &exceptionIndex,
                                              &callTargetReturnIndex, &returnValueIndex, &callTargetStateToken,
                                              &exceptionToken, &callTargetReturnToken, &firstInstruction);

    // ***
    // BEGIN METHOD PART
    // ***

    // *** Load instance into the stack (if not static)
    if (isStatic)
    {
        if (caller->type.valueType)
        {
            // Static methods in a ValueType can't be instrumented.
            // In the future this can be supported by adding a local for the valuetype and initialize it to the default
            // value. After the signature modification we need to emit the following IL to initialize and load into the
            // stack.
            //    ldloca.s [localIndex]
            //    initobj [valueType]
            //    ldloc.s [localIndex]
            Logger::Warn("*** CallTarget_RewriterCallback(): Static methods in a ValueType cannot be instrumented. ");
            return S_FALSE;
        }
        reWriterWrapper.LoadNull();
    }
    else
    {
        reWriterWrapper.LoadArgument(0);
        if (caller->type.valueType)
        {
            if (caller->type.type_spec != mdTypeSpecNil)
            {
                reWriterWrapper.LoadObj(caller->type.type_spec);
            }
            else if (!caller->type.isGeneric)
            {
                reWriterWrapper.LoadObj(caller->type.id);
            }
            else
            {
                // Generic struct instrumentation is not supported
                // IMetaDataImport::GetMemberProps and IMetaDataImport::GetMemberRefProps returns
                // The parent token as mdTypeDef and not as a mdTypeSpec
                // that's because the method definition is stored in the mdTypeDef
                // The problem is that we don't have the exact Spec of that generic
                // We can't emit LoadObj or Box because that would result in an invalid IL.
                // This problem doesn't occur on a class type because we can always relay in the
                // object type.
                return S_FALSE;
            }
        }
    }

    // *** Load the method arguments to the stack
    if (is_integration_method)
    {
        if (numArgs < FASTPATH_COUNT)
        {
            // Load the arguments directly (FastPath)
            for (int i = 0; i < numArgs; i++)
            {
                const auto [elementType, argTypeFlags] = methodArguments[i].GetElementTypeAndFlags();
                if (corProfiler->enable_by_ref_instrumentation)
                {
                    if (argTypeFlags & TypeFlagByRef)
                    {
                        reWriterWrapper.LoadArgument(i + (isStatic ? 0 : 1));
                    }
                    else
                    {
                        reWriterWrapper.LoadArgumentRef(i + (isStatic ? 0 : 1));
                    }
                }
                else
                {
                    reWriterWrapper.LoadArgument(i + (isStatic ? 0 : 1));
                    if (argTypeFlags & TypeFlagByRef)
                    {
                        Logger::Warn("*** CallTarget_RewriterCallback(): Methods with ref parameters "
                                     "cannot be instrumented. ");
                        return S_FALSE;
                    }
                }
            }
        }
        else
        {
            // Load the arguments inside an object array (SlowPath)
            reWriterWrapper.CreateArray(tracerTokens->GetObjectTypeRef(), numArgs);
            for (int i = 0; i < numArgs; i++)
            {
                reWriterWrapper.BeginLoadValueIntoArray(i);
                reWriterWrapper.LoadArgument(i + (isStatic ? 0 : 1));
                const auto [elementType, argTypeFlags] = methodArguments[i].GetElementTypeAndFlags();
                if (argTypeFlags & TypeFlagByRef)
                {
                    Logger::Warn("*** CallTarget_RewriterCallback(): Methods with ref parameters "
                                 "cannot be instrumented. ");
                    return S_FALSE;
                }
                if (argTypeFlags & TypeFlagBoxedType)
                {
                    const auto& tok = methodArguments[i].GetTypeTok(metaEmit, tracerTokens->GetCorLibAssemblyRef());
                    if (tok == mdTokenNil)
                    {
                        return S_FALSE;
                    }
                    reWriterWrapper.Box(tok);
                }
                reWriterWrapper.EndLoadValueIntoArray();
            }
        }
    }
    else
    {
        // Load the methodDef token to produce a RuntimeMethodHandle on the stack
        reWriterWrapper.LoadToken(caller->id);

        runtimeMethodHandleBuffer[0] = ELEMENT_TYPE_VALUETYPE;
        ULONG runtimeMethodHandleTokenLength =
            CorSigCompressToken(tracerTokens->GetRuntimeMethodHandleTypeRef(), &runtimeMethodHandleBuffer[1]);

        // Load the typeDef token to produce a RuntimeTypeHandle on the stack
        reWriterWrapper.LoadToken(caller->type.id);

        runtimeTypeHandleBuffer[0] = ELEMENT_TYPE_VALUETYPE;
        ULONG runtimeTypeHandleTokenLength =
            CorSigCompressToken(tracerTokens->GetRuntimeTypeHandleTypeRef(), &runtimeTypeHandleBuffer[1]);

        // Replace method arguments with one RuntimeMethodHandle argument and one RuntimeTypeHandle argument
        trace::TypeSignature runtimeMethodHandleArgument{};
        runtimeMethodHandleArgument.pbBase = runtimeMethodHandleBuffer;
        runtimeMethodHandleArgument.length = runtimeMethodHandleTokenLength + 1;
        runtimeMethodHandleArgument.offset = 0;
        traceAnnotationArguments.push_back(runtimeMethodHandleArgument);

        trace::TypeSignature runtimeTypeHandleArgument{};
        runtimeTypeHandleArgument.pbBase = runtimeTypeHandleBuffer;
        runtimeTypeHandleArgument.length = runtimeTypeHandleTokenLength + 1;
        runtimeTypeHandleArgument.offset = 0;
        traceAnnotationArguments.push_back(runtimeTypeHandleArgument);

        methodArguments = traceAnnotationArguments;
    }

    // *** Emit BeginMethod call
    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("Caller Type.Id: ", HexStr(&caller->type.id, sizeof(mdToken)));
        Logger::Debug("Caller Type.IsGeneric: ", caller->type.isGeneric);
        Logger::Debug("Caller Type.IsValid: ", caller->type.IsValid());
        Logger::Debug("Caller Type.Name: ", caller->type.name);
        Logger::Debug("Caller Type.TokenType: ", caller->type.token_type);
        Logger::Debug("Caller Type.Spec: ", HexStr(&caller->type.type_spec, sizeof(mdTypeSpec)));
        Logger::Debug("Caller Type.ValueType: ", caller->type.valueType);
        //
        if (caller->type.extend_from != nullptr)
        {
            Logger::Debug("Caller Type Extend From.Id: ", HexStr(&caller->type.extend_from->id, sizeof(mdToken)));
            Logger::Debug("Caller Type Extend From.IsGeneric: ", caller->type.extend_from->isGeneric);
            Logger::Debug("Caller Type Extend From.IsValid: ", caller->type.extend_from->IsValid());
            Logger::Debug("Caller Type Extend From.Name: ", caller->type.extend_from->name);
            Logger::Debug("Caller Type Extend From.TokenType: ", caller->type.extend_from->token_type);
            Logger::Debug("Caller Type Extend From.Spec: ",
                          HexStr(&caller->type.extend_from->type_spec, sizeof(mdTypeSpec)));
            Logger::Debug("Caller Type Extend From.ValueType: ", caller->type.extend_from->valueType);
        }
        //
        if (caller->type.parent_type != nullptr)
        {
            Logger::Debug("Caller ParentType.Id: ", HexStr(&caller->type.parent_type->id, sizeof(mdToken)));
            Logger::Debug("Caller ParentType.IsGeneric: ", caller->type.parent_type->isGeneric);
            Logger::Debug("Caller ParentType.IsValid: ", caller->type.parent_type->IsValid());
            Logger::Debug("Caller ParentType.Name: ", caller->type.parent_type->name);
            Logger::Debug("Caller ParentType.TokenType: ", caller->type.parent_type->token_type);
            Logger::Debug("Caller ParentType.Spec: ", HexStr(&caller->type.parent_type->type_spec, sizeof(mdTypeSpec)));
            Logger::Debug("Caller ParentType.ValueType: ", caller->type.parent_type->valueType);
        }
    }

    ILInstr* beginCallInstruction;
    hr = tracerTokens->WriteBeginMethod(&reWriterWrapper, integration_type_ref, &caller->type, methodArguments,
                                        ignoreByRefInstrumentation, &beginCallInstruction);
    if (FAILED(hr))
    {
        // Error message is written to the log in WriteBeginMethod.
        return S_FALSE;
    }
    reWriterWrapper.StLocal(callTargetStateIndex);
    ILInstr* pStateLeaveToBeginOriginalMethodInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** BeginMethod call catch
    ILInstr* beginMethodCatchFirstInstr = nullptr;
    tracerTokens->WriteLogException(&reWriterWrapper, integration_type_ref, &caller->type, &beginMethodCatchFirstInstr);
    ILInstr* beginMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** BeginMethod exception handling clause
    EHClause beginMethodExClause{};
    beginMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    beginMethodExClause.m_pTryBegin     = firstInstruction;
    beginMethodExClause.m_pTryEnd       = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerBegin = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerEnd   = beginMethodCatchLeaveInstr;
    beginMethodExClause.m_ClassToken    = tracerTokens->GetExceptionTypeRef();

    // ***
    // METHOD EXECUTION
    // ***
    ILInstr* beginOriginalMethodInstr                = reWriterWrapper.GetCurrentILInstr();
    pStateLeaveToBeginOriginalMethodInstr->m_pTarget = beginOriginalMethodInstr;
    beginMethodCatchLeaveInstr->m_pTarget            = beginOriginalMethodInstr;

    // ***
    // ENDING OF THE METHOD EXECUTION
    // ***

    // *** Create return instruction and insert it at the end
    ILInstr* methodReturnInstr  = rewriter.NewILInstr();
    methodReturnInstr->m_opcode = CEE_RET;
    rewriter.InsertAfter(rewriter.GetILList()->m_pPrev, methodReturnInstr);
    reWriterWrapper.SetILPosition(methodReturnInstr);

    // ***
    // EXCEPTION CATCH
    // ***
    ILInstr* startExceptionCatch = reWriterWrapper.StLocal(exceptionIndex);
    reWriterWrapper.SetILPosition(methodReturnInstr);
    ILInstr* rethrowInstr = reWriterWrapper.Rethrow();

    // ***
    // EXCEPTION FINALLY / END METHOD PART
    // ***
    ILInstr* endMethodTryStartInstr;

    // *** Load instance into the stack (if not static)
    if (isStatic)
    {
        if (caller->type.valueType)
        {
            // Static methods in a ValueType can't be instrumented.
            // In the future this can be supported by adding a local for the valuetype
            // and initialize it to the default value. After the signature
            // modification we need to emit the following IL to initialize and load
            // into the stack.
            //    ldloca.s [localIndex]
            //    initobj [valueType]
            //    ldloc.s [localIndex]
            Logger::Warn("CallTarget_RewriterCallback: Static methods in a ValueType cannot "
                         "be instrumented. ");
            return S_FALSE;
        }
        endMethodTryStartInstr = reWriterWrapper.LoadNull();
    }
    else
    {
        endMethodTryStartInstr = reWriterWrapper.LoadArgument(0);
        if (caller->type.valueType)
        {
            if (caller->type.type_spec != mdTypeSpecNil)
            {
                reWriterWrapper.LoadObj(caller->type.type_spec);
            }
            else if (!caller->type.isGeneric)
            {
                reWriterWrapper.LoadObj(caller->type.id);
            }
            else
            {
                // Generic struct instrumentation is not supported
                // IMetaDataImport::GetMemberProps and IMetaDataImport::GetMemberRefProps returns
                // The parent token as mdTypeDef and not as a mdTypeSpec
                // that's because the method definition is stored in the mdTypeDef
                // The problem is that we don't have the exact Spec of that generic
                // We can't emit LoadObj or Box because that would result in an invalid IL.
                // This problem doesn't occur on a class type because we can always relay in the
                // object type.
                return S_FALSE;
            }
        }
    }

    // *** Load the return value is is not void
    if (!isVoid)
    {
        reWriterWrapper.LoadLocal(returnValueIndex);
    }

    reWriterWrapper.LoadLocal(exceptionIndex);
    if (corProfiler->enable_calltarget_state_by_ref)
    {
        reWriterWrapper.LoadLocalAddress(callTargetStateIndex);
    }
    else
    {
        reWriterWrapper.LoadLocal(callTargetStateIndex);
    }

    ILInstr* endMethodCallInstr;
    if (isVoid)
    {
        tracerTokens->WriteEndVoidReturnMemberRef(&reWriterWrapper, integration_type_ref, &caller->type,
                                                  &endMethodCallInstr);
    }
    else
    {
        tracerTokens->WriteEndReturnMemberRef(&reWriterWrapper, integration_type_ref, &caller->type, &retFuncArg,
                                              &endMethodCallInstr);
    }
    reWriterWrapper.StLocal(callTargetReturnIndex);

    if (!isVoid)
    {
        ILInstr* callTargetReturnGetReturnInstr;
        reWriterWrapper.LoadLocalAddress(callTargetReturnIndex);
        tracerTokens->WriteCallTargetReturnGetReturnValue(&reWriterWrapper, callTargetReturnToken,
                                                          &callTargetReturnGetReturnInstr);
        reWriterWrapper.StLocal(returnValueIndex);
    }

    ILInstr* endMethodTryLeave = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** EndMethod call catch
    ILInstr* endMethodCatchFirstInstr = nullptr;
    tracerTokens->WriteLogException(&reWriterWrapper, integration_type_ref, &caller->type, &endMethodCatchFirstInstr);
    ILInstr* endMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** EndMethod exception handling clause
    EHClause endMethodExClause{};
    endMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    endMethodExClause.m_pTryBegin     = endMethodTryStartInstr;
    endMethodExClause.m_pTryEnd       = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerBegin = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerEnd   = endMethodCatchLeaveInstr;
    endMethodExClause.m_ClassToken    = tracerTokens->GetExceptionTypeRef();

    // *** EndMethod leave to finally
    ILInstr* endFinallyInstr            = reWriterWrapper.EndFinally();
    endMethodTryLeave->m_pTarget        = endFinallyInstr;
    endMethodCatchLeaveInstr->m_pTarget = endFinallyInstr;

    // ***
    // METHOD RETURN
    // ***

    // Load the current return value from the local var
    if (!isVoid)
    {
        reWriterWrapper.LoadLocal(returnValueIndex);
    }

    // Changes all returns to a LEAVE.S
    for (ILInstr* pInstr = rewriter.GetILList()->m_pNext; pInstr != rewriter.GetILList(); pInstr = pInstr->m_pNext)
    {
        switch (pInstr->m_opcode)
        {
            case CEE_RET:
            {
                if (pInstr != methodReturnInstr)
                {
                    if (!isVoid)
                    {
                        reWriterWrapper.SetILPosition(pInstr);
                        reWriterWrapper.StLocal(returnValueIndex);
                    }
                    pInstr->m_opcode  = CEE_LEAVE_S;
                    pInstr->m_pTarget = endFinallyInstr->m_pNext;
                }
                break;
            }
            default:
                break;
        }
    }

    // Exception handling clauses
    EHClause exClause{};
    exClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    exClause.m_pTryBegin     = firstInstruction;
    exClause.m_pTryEnd       = startExceptionCatch;
    exClause.m_pHandlerBegin = startExceptionCatch;
    exClause.m_pHandlerEnd   = rethrowInstr;
    exClause.m_ClassToken    = tracerTokens->GetExceptionTypeRef();

    EHClause finallyClause{};
    finallyClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_FINALLY;
    finallyClause.m_pTryBegin     = firstInstruction;
    finallyClause.m_pTryEnd       = rethrowInstr->m_pNext;
    finallyClause.m_pHandlerBegin = rethrowInstr->m_pNext;
    finallyClause.m_pHandlerEnd   = endFinallyInstr;

    // ***
    // Update and Add exception clauses
    // ***
    auto ehCount      = rewriter.GetEHCount();
    auto ehPointer    = rewriter.GetEHPointer();
    auto newEHClauses = new EHClause[ehCount + 4];
    for (unsigned i = 0; i < ehCount; i++)
    {
        newEHClauses[i] = ehPointer[i];
    }

    // *** Add the new EH clauses
    ehCount += 4;
    newEHClauses[ehCount - 4] = beginMethodExClause;
    newEHClauses[ehCount - 3] = endMethodExClause;
    newEHClauses[ehCount - 2] = exClause;
    newEHClauses[ehCount - 1] = finallyClause;
    rewriter.SetEHClause(newEHClauses, ehCount);

    if (IsDumpILRewriteEnabled())
    {
        Logger::Info(original_code);
        Logger::Info(corProfiler->GetILCodes("*** Rewriter(): Modified Code: ", &rewriter, *caller,
                                             module_metadata.metadata_import));
    }

    hr = rewriter.Export();

    if (FAILED(hr))
    {
        Logger::Warn("*** CallTarget_RewriterCallback(): Call to ILRewriter.Export() failed for "
                     "ModuleID=",
                     module_id, " ", function_token);
        return S_FALSE;
    }

    Logger::Info("*** CallTarget_RewriterCallback() Finished: ", caller->type.name, ".", caller->name,
                 "() [IsVoid=", isVoid, ", IsStatic=", isStatic,
                 ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs, "]");
    return S_OK;
}

} // namespace trace

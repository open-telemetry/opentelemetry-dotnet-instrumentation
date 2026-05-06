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
#include "util.h"
#include "version.h"
#include "environment_variables_util.h"

namespace trace
{

namespace
{
const WSTRING calltarget_trampoline_type_name = WStr("__OTelCallTargetTrampoline__");
const WSTRING calltarget_trampoline_indexer_type_name = WStr("__OTelCallTargetIndexer`1");
const WSTRING calltarget_trampoline_state_type_name = WStr("__OTelCallTargetState__");
const WSTRING calltarget_trampoline_return_type_name = WStr("__OTelCallTargetReturn__");
const WSTRING calltarget_trampoline_return_generic_type_name = WStr("__OTelCallTargetReturn__`1");

struct CallTargetTrampolineTokens
{
    mdAssemblyRef corlibRef = mdAssemblyRefNil;
    mdToken objectType = mdTokenNil;
    mdToken exceptionType = mdTokenNil;
    mdToken stateType = mdTokenNil;
    mdToken returnVoidType = mdTokenNil;
    mdToken returnGenericType = mdTokenNil;
    mdToken indexerType = mdTokenNil;
    mdToken trampolineType = mdTokenNil;
    int integrationIndex = -1;
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
    IfFailRet(get_type(calltarget_trampoline_state_type_name.c_str(), &tokens.stateType));
    IfFailRet(get_type(calltarget_trampoline_return_type_name.c_str(), &tokens.returnVoidType));
    IfFailRet(get_type(calltarget_trampoline_return_generic_type_name.c_str(), &tokens.returnGenericType));
    IfFailRet(get_type(calltarget_trampoline_indexer_type_name.c_str(), &tokens.indexerType));
    IfFailRet(get_type(calltarget_trampoline_type_name.c_str(), &tokens.trampolineType));

    tokens.integrationIndex = trace::profiler->GetCallTargetTrampolineIntegrationIndex(*integrationDefinition);

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

void AppendTypeToken(std::vector<COR_SIGNATURE>& signature, mdToken token, bool isValueType)
{
    signature.push_back(isValueType ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);
    AppendCompressedToken(signature, token);
}

void AppendTypeVariable(std::vector<COR_SIGNATURE>& signature, ULONG index, bool byRef = false)
{
    if (byRef)
    {
        signature.push_back(ELEMENT_TYPE_BYREF);
    }

    signature.push_back(ELEMENT_TYPE_VAR);
    AppendCompressedData(signature, index);
}

void AppendMethodVariable(std::vector<COR_SIGNATURE>& signature, ULONG index, bool byRef = false)
{
    if (byRef)
    {
        signature.push_back(ELEMENT_TYPE_BYREF);
    }

    signature.push_back(ELEMENT_TYPE_MVAR);
    AppendCompressedData(signature, index);
}

void AppendTypeSignature(std::vector<COR_SIGNATURE>& signature, const TypeSignature& typeSignature, bool stripByRef)
{
    PCCOR_SIGNATURE rawSignature = nullptr;
    ULONG           rawSignatureLength = typeSignature.GetSignature(rawSignature);
    if (stripByRef && rawSignatureLength > 0 && rawSignature[0] == ELEMENT_TYPE_BYREF)
    {
        rawSignature++;
        rawSignatureLength--;
    }

    signature.insert(signature.end(), rawSignature, rawSignature + rawSignatureLength);
}

std::vector<COR_SIGNATURE> GetCurrentTypeSignature(const TypeInfo* currentType,
                                                   const CallTargetTrampolineTokens& tokens)
{
    bool    isValueType = currentType->valueType;
    mdToken typeToken   = currentType->type_spec;
    if (typeToken == mdTypeSpecNil)
    {
        const TypeInfo* cType = currentType;
        while (!cType->isGeneric)
        {
            if (cType->parent_type == nullptr)
            {
                typeToken = cType->id;
                break;
            }

            cType = cType->parent_type.get();
        }

        if (typeToken == mdTypeSpecNil)
        {
            typeToken   = tokens.objectType;
            isValueType = false;
        }
    }

    std::vector<COR_SIGNATURE> signature;
    AppendTypeToken(signature, typeToken, isValueType);
    return signature;
}

std::vector<COR_SIGNATURE> BuildIndexerMapSignature(const CallTargetTrampolineTokens& tokens)
{
    std::vector<COR_SIGNATURE> current{ELEMENT_TYPE_OBJECT};
    for (int i = 0; i <= tokens.integrationIndex; i++)
    {
        std::vector<COR_SIGNATURE> next;
        next.push_back(ELEMENT_TYPE_GENERICINST);
        AppendTypeToken(next, tokens.indexerType, false);
        AppendCompressedData(next, 1);
        next.insert(next.end(), current.begin(), current.end());
        current = next;
    }

    return current;
}

HRESULT GetTypeSpecFromSignature(ModuleMetadata& moduleMetadata,
                                 const std::vector<COR_SIGNATURE>& signature,
                                 mdTypeSpec* typeSpec)
{
    return moduleMetadata.metadata_emit->GetTokenFromTypeSpec(signature.data(), static_cast<ULONG>(signature.size()),
                                                             typeSpec);
}

std::vector<COR_SIGNATURE> BuildGenericInstanceSignature(mdToken openGenericType,
                                                         bool isValueType,
                                                         const std::vector<std::vector<COR_SIGNATURE>>& genericArguments)
{
    std::vector<COR_SIGNATURE> signature;
    signature.push_back(ELEMENT_TYPE_GENERICINST);
    AppendTypeToken(signature, openGenericType, isValueType);
    AppendCompressedData(signature, static_cast<ULONG>(genericArguments.size()));
    for (const auto& argument : genericArguments)
    {
        signature.insert(signature.end(), argument.begin(), argument.end());
    }

    return signature;
}

HRESULT BuildGenericMethodSpec(ModuleMetadata& moduleMetadata,
                               mdMemberRef methodRef,
                               const std::vector<std::vector<COR_SIGNATURE>>& genericArguments,
                               mdMethodSpec* methodSpec)
{
    std::vector<COR_SIGNATURE> signature;
    signature.push_back(IMAGE_CEE_CS_CALLCONV_GENERICINST);
    AppendCompressedData(signature, static_cast<ULONG>(genericArguments.size()));
    for (const auto& argument : genericArguments)
    {
        signature.insert(signature.end(), argument.begin(), argument.end());
    }

    return moduleMetadata.metadata_emit->DefineMethodSpec(methodRef, signature.data(),
                                                          static_cast<ULONG>(signature.size()), methodSpec);
}

HRESULT BuildTrampolineReturnSignature(const CallTargetTrampolineTokens& tokens,
                                       const std::vector<COR_SIGNATURE>& returnSignature,
                                       std::vector<COR_SIGNATURE>& returnVesselSignature)
{
    returnVesselSignature = BuildGenericInstanceSignature(tokens.returnGenericType, true, {returnSignature});
    return S_OK;
}

HRESULT BuildCallTargetReturnTypeSpec(ModuleMetadata& moduleMetadata,
                                      const CallTargetTrampolineTokens& tokens,
                                      const std::vector<COR_SIGNATURE>& returnSignature,
                                      mdTypeSpec* returnTypeSpec)
{
    HRESULT hr = S_OK;
    std::vector<COR_SIGNATURE> signature;
    IfFailRet(BuildTrampolineReturnSignature(tokens, returnSignature, signature));
    return GetTypeSpecFromSignature(moduleMetadata, signature, returnTypeSpec);
}

// Extends the target method local signature and emits the trampoline prologue:
//
// TReturn otelReturnValue = default; // only for non-void methods
// Exception otelException = null;
// __OTelCallTargetReturn__[<TReturn>] otelReturn = default;
// __OTelCallTargetState__ otelState = default;
HRESULT ModifyLocalSigAndInitializeForTrampoline(ILRewriterWrapper&            reWriterWrapper,
                                                 ModuleMetadata&               moduleMetadata,
                                                 FunctionInfo*                 functionInfo,
                                                 CallTargetTrampolineTokens&   tokens,
                                                 ULONG*                        exceptionIndex,
                                                 ULONG*                        stateIndex,
                                                 ULONG*                        callTargetReturnIndex,
                                                 ULONG*                        returnValueIndex,
                                                 mdToken*                      callTargetReturnToken,
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

    ULONG newLocalsToAdd = isVoid ? 3 : 4;
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

    if (!isVoid)
    {
        std::vector<COR_SIGNATURE> returnSignature(returnSignatureType, returnSignatureType + returnSignatureTypeSize);
        mdTypeSpec returnTypeSpec = mdTypeSpecNil;
        IfFailRet(BuildCallTargetReturnTypeSpec(moduleMetadata, tokens, returnSignature, &returnTypeSpec));
        PCCOR_SIGNATURE returnTypeSpecSignature = nullptr;
        ULONG returnTypeSpecSignatureSize = 0;
        IfFailRet(moduleMetadata.metadata_import->GetTypeSpecFromToken(returnTypeSpec, &returnTypeSpecSignature,
                                                                       &returnTypeSpecSignatureSize));
        newSignature.insert(newSignature.end(), returnTypeSpecSignature,
                            returnTypeSpecSignature + returnTypeSpecSignatureSize);
        *callTargetReturnToken = returnTypeSpec;
    }
    else
    {
        AppendTypeToken(newSignature, tokens.returnVoidType, true);
        *callTargetReturnToken = tokens.returnVoidType;
    }

    AppendTypeToken(newSignature, tokens.stateType, true);

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
        *returnValueIndex = newLocalsCount - 4;
    }
    else
    {
        *returnValueIndex = static_cast<ULONG>(ULONG_MAX);
    }

    *exceptionIndex        = newLocalsCount - 3;
    *callTargetReturnIndex = newLocalsCount - 2;
    *stateIndex            = newLocalsCount - 1;

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
    reWriterWrapper.LoadLocalAddress(*callTargetReturnIndex);
    reWriterWrapper.InitObj(*callTargetReturnToken);
    reWriterWrapper.LoadLocalAddress(*stateIndex);
    reWriterWrapper.InitObj(tokens.stateType);
    return S_OK;
}

// Emits the instance argument for CallTarget methods:
//
// TTarget instance = isStatic ? default : this;
//
// Value-type instances are loaded from the by-ref this pointer and passed as TTarget,
// matching the direct CallTarget stack shape.
bool LoadInstanceForCallTarget(ILRewriterWrapper& reWriterWrapper,
                               FunctionInfo* caller,
                               bool isStatic,
                               const char* operationName,
                               ILInstr** firstInstruction)
{
    if (isStatic)
    {
        if (caller->type.valueType)
        {
            Logger::Warn("*** ", operationName,
                         "(): Static methods in a ValueType cannot be instrumented. ");
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
            Logger::Warn("*** ", operationName, "(): Generic struct instrumentation is not supported. ");
            return false;
        }

        reWriterWrapper.LoadObj(valueTypeToken);
    }

    return true;
}

void BuildGenericMethodSignature(std::vector<COR_SIGNATURE>& signature,
                                 ULONG genericArgumentCount,
                                 ULONG parameterCount,
                                 const std::vector<COR_SIGNATURE>& returnSignature)
{
    signature.push_back(IMAGE_CEE_CS_CALLCONV_GENERIC);
    AppendCompressedData(signature, genericArgumentCount);
    AppendCompressedData(signature, parameterCount);
    signature.insert(signature.end(), returnSignature.begin(), returnSignature.end());
}

HRESULT WriteTrampolineMethodSpecCall(ILRewriterWrapper&                            reWriterWrapper,
                                      ModuleMetadata&                               moduleMetadata,
                                      CallTargetTrampolineTokens&                   tokens,
                                      const WSTRING&                                methodName,
                                      const std::vector<COR_SIGNATURE>&             memberSignature,
                                      const std::vector<std::vector<COR_SIGNATURE>>& genericArguments,
                                      ILInstr**                                     instruction)
{
    HRESULT hr = S_OK;
    mdMemberRef methodRef = mdMemberRefNil;
    IfFailRet(moduleMetadata.metadata_emit->DefineMemberRef(tokens.trampolineType, methodName.c_str(),
                                                            memberSignature.data(),
                                                            static_cast<ULONG>(memberSignature.size()), &methodRef));

    mdMethodSpec methodSpec = mdMethodSpecNil;
    IfFailRet(BuildGenericMethodSpec(moduleMetadata, methodRef, genericArguments, &methodSpec));
    *instruction = reWriterWrapper.CallMember(methodSpec, false);
    return S_OK;
}

// Emits a call equivalent to:
//
// __OTelCallTargetTrampoline__.BeginMethod<TMapIntegration, TTarget, ...TArgs>(
//     instance, ref arg1, ..., ref argN)
//
// or, for the slow path:
//
// __OTelCallTargetTrampoline__.BeginMethod<TMapIntegration, TTarget>(instance, args)
HRESULT WriteTrampolineBeginMethod(ILRewriterWrapper&                  reWriterWrapper,
                                   ModuleMetadata&                     moduleMetadata,
                                   CallTargetTrampolineTokens&         tokens,
                                   const TypeInfo*                     currentType,
                                   const std::vector<TypeSignature>&   methodArguments,
                                   ILInstr**                           instruction)
{
    HRESULT hr = S_OK;
    const auto numArguments = static_cast<int>(methodArguments.size());
    const bool slowPath = numArguments >= FASTPATH_COUNT;
    auto mapSignature = BuildIndexerMapSignature(tokens);
    auto targetSignature = GetCurrentTypeSignature(currentType, tokens);

    if (slowPath)
    {
        std::vector<COR_SIGNATURE> signature;
        std::vector<COR_SIGNATURE> stateSignature;
        AppendTypeToken(stateSignature, tokens.stateType, true);
        BuildGenericMethodSignature(signature, 2, 2, stateSignature);
        AppendMethodVariable(signature, 1);
        signature.push_back(ELEMENT_TYPE_SZARRAY);
        signature.push_back(ELEMENT_TYPE_OBJECT);

        return WriteTrampolineMethodSpecCall(reWriterWrapper, moduleMetadata, tokens, WStr("BeginMethod"), signature,
                                             {mapSignature, targetSignature}, instruction);
    }

    std::vector<std::vector<COR_SIGNATURE>> genericArguments{mapSignature, targetSignature};
    for (const auto& methodArgument : methodArguments)
    {
        std::vector<COR_SIGNATURE> argumentSignature;
        const auto [elementType, argTypeFlags] = methodArgument.GetElementTypeAndFlags();
        AppendTypeSignature(argumentSignature, methodArgument, (argTypeFlags & TypeFlagByRef) > 0);
        genericArguments.push_back(argumentSignature);
    }

    std::vector<COR_SIGNATURE> signature;
    std::vector<COR_SIGNATURE> stateSignature;
    AppendTypeToken(stateSignature, tokens.stateType, true);
    BuildGenericMethodSignature(signature, static_cast<ULONG>(2 + numArguments), static_cast<ULONG>(1 + numArguments),
                                stateSignature);
    AppendMethodVariable(signature, 1);
    for (int i = 0; i < numArguments; i++)
    {
        AppendMethodVariable(signature, static_cast<ULONG>(2 + i), true);
    }

    return WriteTrampolineMethodSpecCall(reWriterWrapper, moduleMetadata, tokens, WStr("BeginMethod"), signature,
                                         genericArguments, instruction);
}

// Emits a call equivalent to:
//
// __OTelCallTargetTrampoline__.EndMethod<TMapIntegration, TTarget>(
//     instance, exception, ref state)
HRESULT WriteTrampolineEndVoidMethod(ILRewriterWrapper&          reWriterWrapper,
                                     ModuleMetadata&             moduleMetadata,
                                     CallTargetTrampolineTokens& tokens,
                                     const TypeInfo*             currentType,
                                     ILInstr**                   instruction)
{
    HRESULT hr = S_OK;
    auto mapSignature = BuildIndexerMapSignature(tokens);
    auto targetSignature = GetCurrentTypeSignature(currentType, tokens);

    std::vector<COR_SIGNATURE> signature;
    std::vector<COR_SIGNATURE> returnSignature;
    AppendTypeToken(returnSignature, tokens.returnVoidType, true);
    BuildGenericMethodSignature(signature, 2, 3, returnSignature);
    AppendMethodVariable(signature, 1);
    AppendTypeToken(signature, tokens.exceptionType, false);
    signature.push_back(ELEMENT_TYPE_BYREF);
    AppendTypeToken(signature, tokens.stateType, true);

    return WriteTrampolineMethodSpecCall(reWriterWrapper, moduleMetadata, tokens, WStr("EndMethod"), signature,
                                         {mapSignature, targetSignature}, instruction);
}

// Emits a call equivalent to:
//
// __OTelCallTargetTrampoline__.EndMethod<TMapIntegration, TTarget, TReturn>(
//     instance, returnValue, exception, ref state)
HRESULT WriteTrampolineEndMethod(ILRewriterWrapper&          reWriterWrapper,
                                 ModuleMetadata&             moduleMetadata,
                                 CallTargetTrampolineTokens& tokens,
                                 const TypeInfo*             currentType,
                                 TypeSignature*              returnArgument,
                                 ILInstr**                   instruction)
{
    HRESULT hr = S_OK;
    auto mapSignature = BuildIndexerMapSignature(tokens);
    auto targetSignature = GetCurrentTypeSignature(currentType, tokens);
    std::vector<COR_SIGNATURE> returnSignature;
    AppendTypeSignature(returnSignature, *returnArgument, false);

    std::vector<COR_SIGNATURE> signature;
    std::vector<COR_SIGNATURE> returnMethodVariableSignature;
    AppendMethodVariable(returnMethodVariableSignature, 2);
    std::vector<COR_SIGNATURE> returnVesselSignature;
    IfFailRet(BuildTrampolineReturnSignature(tokens, returnMethodVariableSignature, returnVesselSignature));
    BuildGenericMethodSignature(signature, 3, 4, returnVesselSignature);
    AppendMethodVariable(signature, 1);
    AppendMethodVariable(signature, 2);
    AppendTypeToken(signature, tokens.exceptionType, false);
    signature.push_back(ELEMENT_TYPE_BYREF);
    AppendTypeToken(signature, tokens.stateType, true);

    return WriteTrampolineMethodSpecCall(reWriterWrapper, moduleMetadata, tokens, WStr("EndMethod"), signature,
                                         {mapSignature, targetSignature, returnSignature}, instruction);
}

// Emits a call equivalent to:
//
// callTargetReturn.GetReturnValue()
HRESULT WriteTrampolineReturnGetReturnValue(ILRewriterWrapper&          reWriterWrapper,
                                            ModuleMetadata&             moduleMetadata,
                                            TypeSignature*              returnArgument,
                                            mdTypeSpec                  callTargetReturnTypeSpec,
                                            ILInstr**                   instruction)
{
    HRESULT hr = S_OK;
    std::vector<COR_SIGNATURE> signature;
    signature.push_back(IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS);
    AppendCompressedData(signature, 0);
    AppendTypeVariable(signature, 0);

    mdMemberRef getReturnValueRef = mdMemberRefNil;
    IfFailRet(moduleMetadata.metadata_emit->DefineMemberRef(callTargetReturnTypeSpec, WStr("GetReturnValue"),
                                                            signature.data(), static_cast<ULONG>(signature.size()),
                                                            &getReturnValueRef));
    *instruction = reWriterWrapper.CallMember(getReturnValueRef, false);
    return S_OK;
}

// Emits a call equivalent to:
//
// __OTelCallTargetTrampoline__.LogException<TMapIntegration, TTarget>(exception)
HRESULT WriteTrampolineLogException(ILRewriterWrapper&          reWriterWrapper,
                                    ModuleMetadata&             moduleMetadata,
                                    CallTargetTrampolineTokens& tokens,
                                    const TypeInfo*             currentType,
                                    ILInstr**                   instruction)
{
    HRESULT hr = S_OK;
    auto mapSignature = BuildIndexerMapSignature(tokens);
    auto targetSignature = GetCurrentTypeSignature(currentType, tokens);

    std::vector<COR_SIGNATURE> signature;
    std::vector<COR_SIGNATURE> voidSignature{ELEMENT_TYPE_VOID};
    BuildGenericMethodSignature(signature, 2, 1, voidSignature);
    AppendTypeToken(signature, tokens.exceptionType, false);

    return WriteTrampolineMethodSpecCall(reWriterWrapper, moduleMetadata, tokens, WStr("LogException"), signature,
                                         {mapSignature, targetSignature}, instruction);
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
    bool                   use_trampoline =
        corProfiler->IsCallTargetTrampolineEnabled() && is_integration_method;
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
    auto                              hr         = S_OK;
    const char*                       logPrefix  = use_trampoline ? "CallTarget_Trampoline_RewriterCallback"
                                                                  : "CallTarget_RewriterCallback";

    // *** Get reference to the integration type
    mdTypeRef integration_type_ref = mdTypeRefNil;
    CallTargetTrampolineTokens trampolineTokens;
    if (use_trampoline)
    {
        hr = BuildCallTargetTrampolineTokens(module_metadata, integration_definition, trampolineTokens);
        if (FAILED(hr))
        {
            return S_FALSE;
        }
    }
    else if (!corProfiler->GetIntegrationTypeRef(module_metadata, module_id, *integration_definition,
                                                integration_type_ref))
    {
        Logger::Warn("*** CallTarget_RewriterCallback() skipping method: Integration Type Ref cannot be found for ",
                     " token=", function_token, " caller_name=", caller->type.name, ".", caller->name, "()");
        return S_FALSE;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("*** ", logPrefix, "() Start: ", caller->type.name, ".", caller->name,
                      "() [IsVoid=", isVoid, ", IsStatic=", isStatic, ", Trampoline=", use_trampoline,
                      ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs,
                      "]");
    }

    // First we check if the managed profiler has not been loaded yet
    if (!corProfiler->ProfilerAssemblyIsLoadedIntoAppDomain(module_metadata.app_domain_id))
    {
        Logger::Warn(
            "*** ", logPrefix, "() skipping method: Method replacement found but the managed profiler has "
            "not yet been loaded into AppDomain with id=",
            module_metadata.app_domain_id, " token=", function_token, " caller_name=", caller->type.name, ".",
            caller->name, "()");
        return S_FALSE;
    }

    // *** Create rewriter
    ILRewriter rewriter(corProfiler->info_, methodHandler->GetFunctionControl(), module_id, function_token);
    hr = rewriter.Import();
    if (FAILED(hr))
    {
        Logger::Warn("*** ", logPrefix, "(): Call to ILRewriter.Import() failed for ", module_id, " ",
                     function_token);
        return S_FALSE;
    }

    // *** Store the original il code text if the dump_il option is enabled.
    std::string original_code;
    if (IsDumpILRewriteEnabled())
    {
        original_code = corProfiler->GetILCodes(use_trampoline
                                                    ? "*** CallTarget_Trampoline_RewriterCallback(): Original Code: "
                                                    : "*** CallTarget_RewriterCallback(): Original Code: ",
                                                &rewriter,
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
    if (use_trampoline)
    {
        hr = ModifyLocalSigAndInitializeForTrampoline(reWriterWrapper, module_metadata, caller, trampolineTokens,
                                                      &exceptionIndex, &callTargetStateIndex, &callTargetReturnIndex,
                                                      &returnValueIndex, &callTargetReturnToken, &firstInstruction);
        callTargetStateToken = trampolineTokens.stateType;
        exceptionToken = trampolineTokens.exceptionType;
    }
    else
    {
        hr = tracerTokens->ModifyLocalSigAndInitialize(&reWriterWrapper, caller, &callTargetStateIndex,
                                                       &exceptionIndex, &callTargetReturnIndex, &returnValueIndex,
                                                       &callTargetStateToken, &exceptionToken, &callTargetReturnToken,
                                                       &firstInstruction);
    }
    if (FAILED(hr))
    {
        return S_FALSE;
    }

    // ***
    // BEGIN METHOD PART
    // ***

    // *** Load instance into the stack (if not static)
    ILInstr* beginInstanceLoadInstruction = nullptr;
    if (!LoadInstanceForCallTarget(reWriterWrapper, caller, isStatic, logPrefix, &beginInstanceLoadInstruction))
    {
        return S_FALSE;
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
                if (use_trampoline || corProfiler->enable_by_ref_instrumentation)
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
                        Logger::Warn("*** ", logPrefix, "(): Methods with ref parameters "
                                     "cannot be instrumented. ");
                        return S_FALSE;
                    }
                }
            }
        }
        else
        {
            // Load the arguments inside an object array (SlowPath)
            reWriterWrapper.CreateArray(use_trampoline ? trampolineTokens.objectType : tracerTokens->GetObjectTypeRef(),
                                        numArgs);
            for (int i = 0; i < numArgs; i++)
            {
                reWriterWrapper.BeginLoadValueIntoArray(i);
                reWriterWrapper.LoadArgument(i + (isStatic ? 0 : 1));
                const auto [elementType, argTypeFlags] = methodArguments[i].GetElementTypeAndFlags();
                if (argTypeFlags & TypeFlagByRef)
                {
                    Logger::Warn("*** ", logPrefix, "(): Methods with ref parameters "
                                 "cannot be instrumented. ");
                    return S_FALSE;
                }
                if (argTypeFlags & TypeFlagBoxedType)
                {
                    const auto corLibRef = use_trampoline ? trampolineTokens.corlibRef
                                                          : tracerTokens->GetCorLibAssemblyRef();
                    const auto& tok = methodArguments[i].GetTypeTok(metaEmit, corLibRef);
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
    if (use_trampoline)
    {
        hr = WriteTrampolineBeginMethod(reWriterWrapper, module_metadata, trampolineTokens, &caller->type,
                                        methodArguments, &beginCallInstruction);
    }
    else
    {
        hr = tracerTokens->WriteBeginMethod(&reWriterWrapper, integration_type_ref, &caller->type, methodArguments,
                                            ignoreByRefInstrumentation, &beginCallInstruction);
    }
    if (FAILED(hr))
    {
        // Error message is written to the log in WriteBeginMethod.
        return S_FALSE;
    }
    reWriterWrapper.StLocal(callTargetStateIndex);
    ILInstr* pStateLeaveToBeginOriginalMethodInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** BeginMethod call catch
    ILInstr* beginMethodCatchFirstInstr = nullptr;
    if (use_trampoline)
    {
        hr = WriteTrampolineLogException(reWriterWrapper, module_metadata, trampolineTokens, &caller->type,
                                         &beginMethodCatchFirstInstr);
    }
    else
    {
        hr = tracerTokens->WriteLogException(&reWriterWrapper, integration_type_ref, &caller->type,
                                             &beginMethodCatchFirstInstr);
    }
    if (FAILED(hr))
    {
        return S_FALSE;
    }
    ILInstr* beginMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** BeginMethod exception handling clause
    EHClause beginMethodExClause{};
    beginMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    beginMethodExClause.m_pTryBegin     = firstInstruction;
    beginMethodExClause.m_pTryEnd       = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerBegin = beginMethodCatchFirstInstr;
    beginMethodExClause.m_pHandlerEnd   = beginMethodCatchLeaveInstr;
    beginMethodExClause.m_ClassToken    = exceptionToken;

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
    ILInstr* endMethodTryStartInstr = nullptr;

    // *** Load instance into the stack (if not static)
    if (!LoadInstanceForCallTarget(reWriterWrapper, caller, isStatic, logPrefix, &endMethodTryStartInstr))
    {
        return S_FALSE;
    }

    // *** Load the return value is is not void
    if (!isVoid)
    {
        reWriterWrapper.LoadLocal(returnValueIndex);
    }

    reWriterWrapper.LoadLocal(exceptionIndex);
    if (use_trampoline || corProfiler->enable_calltarget_state_by_ref)
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
        if (use_trampoline)
        {
            hr = WriteTrampolineEndVoidMethod(reWriterWrapper, module_metadata, trampolineTokens, &caller->type,
                                              &endMethodCallInstr);
        }
        else
        {
            hr = tracerTokens->WriteEndVoidReturnMemberRef(&reWriterWrapper, integration_type_ref, &caller->type,
                                                           &endMethodCallInstr);
        }
    }
    else
    {
        if (use_trampoline)
        {
            hr = WriteTrampolineEndMethod(reWriterWrapper, module_metadata, trampolineTokens, &caller->type,
                                          &retFuncArg, &endMethodCallInstr);
        }
        else
        {
            hr = tracerTokens->WriteEndReturnMemberRef(&reWriterWrapper, integration_type_ref, &caller->type,
                                                       &retFuncArg, &endMethodCallInstr);
        }
    }
    if (FAILED(hr))
    {
        return S_FALSE;
    }
    reWriterWrapper.StLocal(callTargetReturnIndex);

    if (!isVoid)
    {
        ILInstr* callTargetReturnGetReturnInstr;
        reWriterWrapper.LoadLocalAddress(callTargetReturnIndex);
        if (use_trampoline)
        {
            hr = WriteTrampolineReturnGetReturnValue(reWriterWrapper, module_metadata, &retFuncArg,
                                                     static_cast<mdTypeSpec>(callTargetReturnToken),
                                                     &callTargetReturnGetReturnInstr);
        }
        else
        {
            hr = tracerTokens->WriteCallTargetReturnGetReturnValue(&reWriterWrapper,
                                                                   static_cast<mdTypeSpec>(callTargetReturnToken),
                                                                   &callTargetReturnGetReturnInstr);
        }
        if (FAILED(hr))
        {
            return S_FALSE;
        }
        reWriterWrapper.StLocal(returnValueIndex);
    }

    ILInstr* endMethodTryLeave = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** EndMethod call catch
    ILInstr* endMethodCatchFirstInstr = nullptr;
    if (use_trampoline)
    {
        hr = WriteTrampolineLogException(reWriterWrapper, module_metadata, trampolineTokens, &caller->type,
                                         &endMethodCatchFirstInstr);
    }
    else
    {
        hr = tracerTokens->WriteLogException(&reWriterWrapper, integration_type_ref, &caller->type,
                                             &endMethodCatchFirstInstr);
    }
    if (FAILED(hr))
    {
        return S_FALSE;
    }
    ILInstr* endMethodCatchLeaveInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** EndMethod exception handling clause
    EHClause endMethodExClause{};
    endMethodExClause.m_Flags         = COR_ILEXCEPTION_CLAUSE_NONE;
    endMethodExClause.m_pTryBegin     = endMethodTryStartInstr;
    endMethodExClause.m_pTryEnd       = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerBegin = endMethodCatchFirstInstr;
    endMethodExClause.m_pHandlerEnd   = endMethodCatchLeaveInstr;
    endMethodExClause.m_ClassToken    = exceptionToken;

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
    exClause.m_ClassToken    = exceptionToken;

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
        Logger::Info(corProfiler->GetILCodes(use_trampoline
                                                 ? "*** CallTarget_Trampoline_RewriterCallback(): Modified Code: "
                                                 : "*** Rewriter(): Modified Code: ",
                                             &rewriter, *caller,
                                             module_metadata.metadata_import));
    }

    hr = rewriter.Export();

    if (FAILED(hr))
    {
        Logger::Warn("*** ", logPrefix, "(): Call to ILRewriter.Export() failed for "
                     "ModuleID=",
                     module_id, " ", function_token);
        return S_FALSE;
    }

    Logger::Info("*** ", logPrefix, "() Finished: ", caller->type.name, ".", caller->name,
                 "() [IsVoid=", isVoid, ", IsStatic=", isStatic, ", Trampoline=", use_trampoline,
                 ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs, "]");
    return S_OK;
}

} // namespace trace

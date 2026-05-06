/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "calltarget_rewrite_tokens.h"

#include "cor_profiler.h"
#include "il_rewriter_wrapper.h"
#include "logger.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"

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

class DirectCallTargetRewriteTokens final : public CallTargetRewriteTokens
{
private:
    TracerTokens* tracerTokens_;
    mdTypeRef integrationTypeRef_ = mdTypeRefNil;
    bool ignoreByRefInstrumentation_;
    bool enableByRefInstrumentation_;
    bool enableCallTargetStateByRef_;

public:
    DirectCallTargetRewriteTokens(TracerTokens* tracerTokens,
                                  mdTypeRef integrationTypeRef,
                                  bool ignoreByRefInstrumentation,
                                  bool enableByRefInstrumentation,
                                  bool enableCallTargetStateByRef) :
        tracerTokens_(tracerTokens),
        integrationTypeRef_(integrationTypeRef),
        ignoreByRefInstrumentation_(ignoreByRefInstrumentation),
        enableByRefInstrumentation_(enableByRefInstrumentation),
        enableCallTargetStateByRef_(enableCallTargetStateByRef)
    {
    }

    const char* OperationName() const override
    {
        return "CallTarget_RewriterCallback";
    }

    bool IsTrampoline() const override
    {
        return false;
    }

    bool ShouldPassFastArgumentByRef() const override
    {
        return !ignoreByRefInstrumentation_ && enableByRefInstrumentation_;
    }

    bool ShouldPassStateByRef() const override
    {
        return enableCallTargetStateByRef_;
    }

    mdToken GetObjectType() override
    {
        return tracerTokens_->GetObjectTypeRef();
    }

    mdAssemblyRef GetCorLibAssemblyRef() override
    {
        return tracerTokens_->GetCorLibAssemblyRef();
    }

    HRESULT ModifyLocalSigAndInitialize(ILRewriterWrapper& reWriterWrapper,
                                        FunctionInfo* caller,
                                        ULONG* callTargetStateIndex,
                                        ULONG* exceptionIndex,
                                        ULONG* callTargetReturnIndex,
                                        ULONG* returnValueIndex,
                                        mdToken* callTargetStateToken,
                                        mdToken* exceptionToken,
                                        mdToken* callTargetReturnToken,
                                        ILInstr** firstInstruction) override
    {
        return tracerTokens_->ModifyLocalSigAndInitialize(&reWriterWrapper, caller, callTargetStateIndex,
                                                          exceptionIndex, callTargetReturnIndex, returnValueIndex,
                                                          callTargetStateToken, exceptionToken, callTargetReturnToken,
                                                          firstInstruction);
    }

    HRESULT WriteBeginMethod(ILRewriterWrapper& reWriterWrapper,
                             const TypeInfo* currentType,
                             const std::vector<TypeSignature>& methodArguments,
                             ILInstr** instruction) override
    {
        return tracerTokens_->WriteBeginMethod(&reWriterWrapper, integrationTypeRef_, currentType, methodArguments,
                                               ignoreByRefInstrumentation_, instruction);
    }

    HRESULT WriteEndVoidMethod(ILRewriterWrapper& reWriterWrapper,
                               const TypeInfo* currentType,
                               ILInstr** instruction) override
    {
        return tracerTokens_->WriteEndVoidReturnMemberRef(&reWriterWrapper, integrationTypeRef_, currentType,
                                                          instruction);
    }

    HRESULT WriteEndMethod(ILRewriterWrapper& reWriterWrapper,
                           const TypeInfo* currentType,
                           TypeSignature* returnArgument,
                           ILInstr** instruction) override
    {
        return tracerTokens_->WriteEndReturnMemberRef(&reWriterWrapper, integrationTypeRef_, currentType,
                                                      returnArgument, instruction);
    }

    HRESULT WriteCallTargetReturnGetReturnValue(ILRewriterWrapper& reWriterWrapper,
                                                TypeSignature*,
                                                mdToken callTargetReturnToken,
                                                ILInstr** instruction) override
    {
        return tracerTokens_->WriteCallTargetReturnGetReturnValue(&reWriterWrapper,
                                                                  static_cast<mdTypeSpec>(callTargetReturnToken),
                                                                  instruction);
    }

    HRESULT WriteLogException(ILRewriterWrapper& reWriterWrapper,
                              const TypeInfo* currentType,
                              ILInstr** instruction) override
    {
        return tracerTokens_->WriteLogException(&reWriterWrapper, integrationTypeRef_, currentType, instruction);
    }
};

class TrampolineCallTargetRewriteTokens final : public CallTargetRewriteTokens
{
private:
    ModuleMetadata& moduleMetadata_;
    IntegrationDefinition* integrationDefinition_;
    CallTargetTrampolineTokens tokens_;

public:
    TrampolineCallTargetRewriteTokens(ModuleMetadata& moduleMetadata, IntegrationDefinition* integrationDefinition) :
        moduleMetadata_(moduleMetadata), integrationDefinition_(integrationDefinition)
    {
    }

    HRESULT Initialize()
    {
        return BuildCallTargetTrampolineTokens(moduleMetadata_, integrationDefinition_, tokens_);
    }

    const char* OperationName() const override
    {
        return "CallTarget_Trampoline_RewriterCallback";
    }

    bool IsTrampoline() const override
    {
        return true;
    }

    bool ShouldPassFastArgumentByRef() const override
    {
        return true;
    }

    bool ShouldPassStateByRef() const override
    {
        return true;
    }

    mdToken GetObjectType() override
    {
        return tokens_.objectType;
    }

    mdAssemblyRef GetCorLibAssemblyRef() override
    {
        return tokens_.corlibRef;
    }

    HRESULT ModifyLocalSigAndInitialize(ILRewriterWrapper& reWriterWrapper,
                                        FunctionInfo* caller,
                                        ULONG* callTargetStateIndex,
                                        ULONG* exceptionIndex,
                                        ULONG* callTargetReturnIndex,
                                        ULONG* returnValueIndex,
                                        mdToken* callTargetStateToken,
                                        mdToken* exceptionToken,
                                        mdToken* callTargetReturnToken,
                                        ILInstr** firstInstruction) override
    {
        auto hr = ModifyLocalSigAndInitializeForTrampoline(reWriterWrapper, moduleMetadata_, caller, tokens_,
                                                           exceptionIndex, callTargetStateIndex,
                                                           callTargetReturnIndex, returnValueIndex,
                                                           callTargetReturnToken, firstInstruction);
        if (FAILED(hr))
        {
            return hr;
        }

        *callTargetStateToken = tokens_.stateType;
        *exceptionToken = tokens_.exceptionType;
        return S_OK;
    }

    HRESULT WriteBeginMethod(ILRewriterWrapper& reWriterWrapper,
                             const TypeInfo* currentType,
                             const std::vector<TypeSignature>& methodArguments,
                             ILInstr** instruction) override
    {
        return WriteTrampolineBeginMethod(reWriterWrapper, moduleMetadata_, tokens_, currentType, methodArguments,
                                          instruction);
    }

    HRESULT WriteEndVoidMethod(ILRewriterWrapper& reWriterWrapper,
                               const TypeInfo* currentType,
                               ILInstr** instruction) override
    {
        return WriteTrampolineEndVoidMethod(reWriterWrapper, moduleMetadata_, tokens_, currentType, instruction);
    }

    HRESULT WriteEndMethod(ILRewriterWrapper& reWriterWrapper,
                           const TypeInfo* currentType,
                           TypeSignature* returnArgument,
                           ILInstr** instruction) override
    {
        return WriteTrampolineEndMethod(reWriterWrapper, moduleMetadata_, tokens_, currentType, returnArgument,
                                        instruction);
    }

    HRESULT WriteCallTargetReturnGetReturnValue(ILRewriterWrapper& reWriterWrapper,
                                                TypeSignature* returnArgument,
                                                mdToken callTargetReturnToken,
                                                ILInstr** instruction) override
    {
        return WriteTrampolineReturnGetReturnValue(reWriterWrapper, moduleMetadata_, returnArgument,
                                                   static_cast<mdTypeSpec>(callTargetReturnToken), instruction);
    }

    HRESULT WriteLogException(ILRewriterWrapper& reWriterWrapper,
                              const TypeInfo* currentType,
                              ILInstr** instruction) override
    {
        return WriteTrampolineLogException(reWriterWrapper, moduleMetadata_, tokens_, currentType, instruction);
    }
};

} // namespace

std::unique_ptr<CallTargetRewriteTokens> CreateDirectCallTargetRewriteTokens(TracerTokens* tracerTokens,
                                                                             mdTypeRef integrationTypeRef,
                                                                             bool ignoreByRefInstrumentation,
                                                                             bool enableByRefInstrumentation,
                                                                             bool enableCallTargetStateByRef)
{
    return std::make_unique<DirectCallTargetRewriteTokens>(tracerTokens, integrationTypeRef,
                                                           ignoreByRefInstrumentation, enableByRefInstrumentation,
                                                           enableCallTargetStateByRef);
}

HRESULT CreateTrampolineCallTargetRewriteTokens(ModuleMetadata& moduleMetadata,
                                                IntegrationDefinition* integrationDefinition,
                                                std::unique_ptr<CallTargetRewriteTokens>* rewriteTokens)
{
    auto tokens = std::make_unique<TrampolineCallTargetRewriteTokens>(moduleMetadata, integrationDefinition);
    auto hr = tokens->Initialize();
    if (FAILED(hr))
    {
        return hr;
    }

    *rewriteTokens = std::move(tokens);
    return S_OK;
}

} // namespace trace

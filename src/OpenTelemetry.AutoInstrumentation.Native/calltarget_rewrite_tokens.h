/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_
#define OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

#include <vector>

#include "clr_helpers.h"
#include "cor.h"
#include "integration.h"
#include "tracer_tokens.h"

class ILRewriterWrapper;

namespace trace
{

struct CallTargetTrampolineTokenRefs
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

class CallTargetTrampolineTokens final : public TracerTokens
{
private:
    IntegrationDefinition* integrationDefinition;
    CallTargetTrampolineTokenRefs tokens;

public:
    CallTargetTrampolineTokens(ModuleMetadata* moduleMetadata, IntegrationDefinition* integrationDefinition);

    HRESULT Initialize();

    mdTypeRef GetObjectTypeRef() override;
    mdTypeRef GetExceptionTypeRef() override;
    mdAssemblyRef GetCorLibAssemblyRef() override;

    bool ShouldLoadArgumentsByRef(const bool ignoreByRefInstrumentation) override;
    bool ShouldLoadCallTargetStateByRef() override;

    HRESULT ModifyLocalSigAndInitialize(void* rewriterWrapperPtr, FunctionInfo* functionInfo,
                                        ULONG* callTargetStateIndex, ULONG* exceptionIndex,
                                        ULONG* callTargetReturnIndex, ULONG* returnValueIndex,
                                        mdToken* callTargetStateToken, mdToken* exceptionToken,
                                        mdToken* callTargetReturnToken, ILInstr** firstInstruction) override;

    HRESULT WriteBeginMethod(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef, const TypeInfo* currentType,
                             const std::vector<TypeSignature>& methodArguments,
                             const bool ignoreByRefInstrumentation, ILInstr** instruction) override;

    HRESULT WriteEndVoidReturnMemberRef(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                        const TypeInfo* currentType, ILInstr** instruction) override;

    HRESULT WriteEndReturnMemberRef(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                    const TypeInfo* currentType, TypeSignature* returnArgument,
                                    ILInstr** instruction) override;

    HRESULT WriteCallTargetReturnGetReturnValue(void* rewriterWrapperPtr, mdTypeSpec callTargetReturnTypeSpec,
                                                ILInstr** instruction) override;

    HRESULT WriteLogException(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef, const TypeInfo* currentType,
                              ILInstr** instruction) override;
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

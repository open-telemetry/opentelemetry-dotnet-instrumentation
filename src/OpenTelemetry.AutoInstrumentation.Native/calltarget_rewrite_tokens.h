/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_
#define OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

#include <memory>
#include <vector>

#include "clr_helpers.h"
#include "cor.h"
#include "integration.h"
#include "tracer_tokens.h"

class ILRewriterWrapper;

namespace trace
{

struct FunctionInfo;
struct TypeInfo;

class CallTargetRewriteTokens
{
public:
    virtual ~CallTargetRewriteTokens() = default;

    virtual const char* OperationName() const = 0;
    virtual bool IsTrampoline() const = 0;
    virtual bool ShouldPassFastArgumentByRef() const = 0;
    virtual bool ShouldPassStateByRef() const = 0;
    virtual mdToken GetObjectType() = 0;
    virtual mdAssemblyRef GetCorLibAssemblyRef() = 0;

    virtual HRESULT ModifyLocalSigAndInitialize(ILRewriterWrapper& reWriterWrapper,
                                                FunctionInfo* caller,
                                                ULONG* callTargetStateIndex,
                                                ULONG* exceptionIndex,
                                                ULONG* callTargetReturnIndex,
                                                ULONG* returnValueIndex,
                                                mdToken* callTargetStateToken,
                                                mdToken* exceptionToken,
                                                mdToken* callTargetReturnToken,
                                                ILInstr** firstInstruction) = 0;

    virtual HRESULT WriteBeginMethod(ILRewriterWrapper& reWriterWrapper,
                                     const TypeInfo* currentType,
                                     const std::vector<TypeSignature>& methodArguments,
                                     ILInstr** instruction) = 0;

    virtual HRESULT WriteEndVoidMethod(ILRewriterWrapper& reWriterWrapper,
                                       const TypeInfo* currentType,
                                       ILInstr** instruction) = 0;

    virtual HRESULT WriteEndMethod(ILRewriterWrapper& reWriterWrapper,
                                   const TypeInfo* currentType,
                                   TypeSignature* returnArgument,
                                   ILInstr** instruction) = 0;

    virtual HRESULT WriteCallTargetReturnGetReturnValue(ILRewriterWrapper& reWriterWrapper,
                                                        TypeSignature* returnArgument,
                                                        mdToken callTargetReturnToken,
                                                        ILInstr** instruction) = 0;

    virtual HRESULT WriteLogException(ILRewriterWrapper& reWriterWrapper,
                                      const TypeInfo* currentType,
                                      ILInstr** instruction) = 0;
};

std::unique_ptr<CallTargetRewriteTokens> CreateDirectCallTargetRewriteTokens(TracerTokens* tracerTokens,
                                                                             mdTypeRef integrationTypeRef,
                                                                             bool ignoreByRefInstrumentation,
                                                                             bool enableByRefInstrumentation,
                                                                             bool enableCallTargetStateByRef);

HRESULT CreateTrampolineCallTargetRewriteTokens(ModuleMetadata& moduleMetadata,
                                                IntegrationDefinition* integrationDefinition,
                                                std::unique_ptr<CallTargetRewriteTokens>* rewriteTokens);

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

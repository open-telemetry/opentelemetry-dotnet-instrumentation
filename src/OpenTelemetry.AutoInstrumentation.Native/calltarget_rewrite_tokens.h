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

HRESULT BuildCallTargetTrampolineTokens(ModuleMetadata& moduleMetadata,
                                        IntegrationDefinition* integrationDefinition,
                                        CallTargetTrampolineTokens& tokens);

HRESULT ModifyLocalSigAndInitializeForTrampoline(ILRewriterWrapper& reWriterWrapper,
                                                 ModuleMetadata& moduleMetadata,
                                                 FunctionInfo* functionInfo,
                                                 CallTargetTrampolineTokens& tokens,
                                                 ULONG* exceptionIndex,
                                                 ULONG* stateIndex,
                                                 ULONG* callTargetReturnIndex,
                                                 ULONG* returnValueIndex,
                                                 mdToken* callTargetReturnToken,
                                                 ILInstr** firstInstruction);

HRESULT WriteTrampolineBeginMethod(ILRewriterWrapper& reWriterWrapper,
                                   ModuleMetadata& moduleMetadata,
                                   CallTargetTrampolineTokens& tokens,
                                   const TypeInfo* currentType,
                                   const std::vector<TypeSignature>& methodArguments,
                                   ILInstr** instruction);

HRESULT WriteTrampolineEndVoidMethod(ILRewriterWrapper& reWriterWrapper,
                                     ModuleMetadata& moduleMetadata,
                                     CallTargetTrampolineTokens& tokens,
                                     const TypeInfo* currentType,
                                     ILInstr** instruction);

HRESULT WriteTrampolineEndMethod(ILRewriterWrapper& reWriterWrapper,
                                 ModuleMetadata& moduleMetadata,
                                 CallTargetTrampolineTokens& tokens,
                                 const TypeInfo* currentType,
                                 TypeSignature* returnArgument,
                                 ILInstr** instruction);

HRESULT WriteTrampolineReturnGetReturnValue(ILRewriterWrapper& reWriterWrapper,
                                            ModuleMetadata& moduleMetadata,
                                            TypeSignature* returnArgument,
                                            mdTypeSpec callTargetReturnTypeSpec,
                                            ILInstr** instruction);

HRESULT WriteTrampolineLogException(ILRewriterWrapper& reWriterWrapper,
                                    ModuleMetadata& moduleMetadata,
                                    CallTargetTrampolineTokens& tokens,
                                    const TypeInfo* currentType,
                                    ILInstr** instruction);

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_TRACER_TOKENS_H_
#define OTEL_CLR_PROFILER_TRACER_TOKENS_H_

#include "calltarget_tokens.h"

#define FASTPATH_COUNT 9

namespace trace
{

class TracerTokens : public CallTargetTokens
{
private:
    mdMemberRef beginArrayMemberRef = mdMemberRefNil;
    mdMemberRef beginMethodFastPathRefs[FASTPATH_COUNT];
    mdMemberRef endVoidMemberRef = mdMemberRefNil;
    mdMemberRef logExceptionRef = mdMemberRefNil;

    HRESULT WriteBeginMethodWithArgumentsArray(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                               const TypeInfo* currentType, ILInstr** instruction);

protected:
    const WSTRING& GetCallTargetType() override;
    const WSTRING& GetCallTargetStateType() override;
    const WSTRING& GetCallTargetReturnType() override;
    const WSTRING& GetCallTargetReturnGenericType() override;

public:
    TracerTokens(ModuleMetadata* module_metadata_ptr);

    virtual mdTypeRef GetObjectTypeRef();
    virtual mdTypeRef GetExceptionTypeRef();
    virtual mdAssemblyRef GetCorLibAssemblyRef();

    virtual bool ShouldLoadArgumentsByRef(const bool ignoreByRefInstrumentation);
    virtual bool ShouldLoadCallTargetStateByRef();

    virtual HRESULT ModifyLocalSigAndInitialize(void* rewriterWrapperPtr, FunctionInfo* functionInfo,
                                                ULONG* callTargetStateIndex, ULONG* exceptionIndex,
                                                ULONG* callTargetReturnIndex, ULONG* returnValueIndex,
                                                mdToken* callTargetStateToken, mdToken* exceptionToken,
                                                mdToken* callTargetReturnToken, ILInstr** firstInstruction);

    virtual HRESULT WriteBeginMethod(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                     const TypeInfo* currentType,
                                     const std::vector<TypeSignature>& methodArguments,
                                     const bool ignoreByRefInstrumentation, ILInstr** instruction);

    virtual HRESULT WriteEndVoidReturnMemberRef(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                                const TypeInfo* currentType, ILInstr** instruction);

    virtual HRESULT WriteEndReturnMemberRef(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                            const TypeInfo* currentType, TypeSignature* returnArgument,
                                            ILInstr** instruction);

    virtual HRESULT WriteCallTargetReturnGetReturnValue(void* rewriterWrapperPtr, mdTypeSpec callTargetReturnTypeSpec,
                                                        ILInstr** instruction);

    virtual HRESULT WriteLogException(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                      const TypeInfo* currentType, ILInstr** instruction);
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_TRACER_TOKENS_H_

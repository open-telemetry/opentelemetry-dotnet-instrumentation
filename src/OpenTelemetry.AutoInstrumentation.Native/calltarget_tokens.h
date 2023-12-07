/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_CALLTARGET_TOKENS_H_
#define OTEL_CLR_PROFILER_CALLTARGET_TOKENS_H_

#include <corhlpr.h>

#include <mutex>
#include <unordered_map>
#include <unordered_set>

#include "clr_helpers.h"
#include "com_ptr.h"
#include "il_rewriter.h"
#include "integration.h"
#include "string.h" // NOLINT

namespace trace
{

/// <summary>
/// Class to control all the token references of the module where the calltarget will be called.
/// Also provides useful helpers for the rewriting process
/// </summary>
class CallTargetTokens
{
private:
    ModuleMetadata* module_metadata_ptr = nullptr;

    // CorLib tokens
    mdAssemblyRef corLibAssemblyRef = mdAssemblyRefNil;
    mdTypeRef objectTypeRef = mdTypeRefNil;
    mdTypeRef typeRef = mdTypeRefNil;
    mdTypeRef runtimeTypeHandleRef = mdTypeRefNil;
    mdToken getTypeFromHandleToken = mdTokenNil;
    mdTypeRef runtimeMethodHandleRef = mdTypeRefNil;

    // CallTarget tokens
    mdAssemblyRef profilerAssemblyRef = mdAssemblyRefNil;

    mdMemberRef callTargetStateTypeGetDefault = mdMemberRefNil;
    mdMemberRef callTargetReturnVoidTypeGetDefault = mdMemberRefNil;
    mdMemberRef getDefaultMemberRef = mdMemberRefNil;

    HRESULT EnsureCorLibTokens();
    mdTypeRef GetTargetStateTypeRef();
    mdTypeRef GetTargetVoidReturnTypeRef();
    mdMemberRef GetCallTargetStateDefaultMemberRef();
    mdMemberRef GetCallTargetReturnVoidDefaultMemberRef();
    mdMemberRef GetCallTargetReturnValueDefaultMemberRef(mdTypeSpec callTargetReturnTypeSpec);
    mdMethodSpec GetCallTargetDefaultValueMethodSpec(TypeSignature* methodArgument);

    HRESULT ModifyLocalSig(ILRewriter* reWriter, TypeSignature* methodReturnValue, ULONG* callTargetStateIndex,
                           ULONG* exceptionIndex, ULONG* callTargetReturnIndex, ULONG* returnValueIndex,
                           mdToken* callTargetStateToken, mdToken* exceptionToken, mdToken* callTargetReturnToken);

    HRESULT WriteBeginMethodWithArgumentsArray(void* rewriterWrapperPtr, mdTypeRef integrationTypeRef,
                                               const TypeInfo* currentType, ILInstr** instruction);

protected:
    // The variables 'enable_by_ref_instrumentation' and 'enable_calltarget_state_by_ref' will always be true,
    // but instead of removing them and the conditional branches they affect, we will keep the variables to make
    // future upstream pulls easier.
    const bool enable_by_ref_instrumentation  = true;
    const bool enable_calltarget_state_by_ref = true;
    mdTypeRef  callTargetTypeRef              = mdTypeRefNil;
    mdTypeRef  callTargetStateTypeRef         = mdTypeRefNil;
    mdTypeRef  callTargetReturnVoidTypeRef    = mdTypeRefNil;
    mdTypeRef  callTargetReturnTypeRef        = mdTypeRefNil;
    mdTypeRef  exTypeRef                      = mdTypeRefNil;

    ModuleMetadata* GetMetadata();
    HRESULT         EnsureBaseCalltargetTokens();
    mdTypeSpec      GetTargetReturnValueTypeRef(TypeSignature* returnArgument);
    mdToken         GetCurrentTypeRef(const TypeInfo* currentType, bool& isValueType);

    virtual const WSTRING& GetCallTargetType()              = 0;
    virtual const WSTRING& GetCallTargetStateType()         = 0;
    virtual const WSTRING& GetCallTargetReturnType()        = 0;
    virtual const WSTRING& GetCallTargetReturnGenericType() = 0;

    CallTargetTokens(ModuleMetadata* moduleMetadataPtr);

public:
    mdTypeRef GetObjectTypeRef();
    mdTypeRef GetExceptionTypeRef();
    mdAssemblyRef GetCorLibAssemblyRef();

    HRESULT ModifyLocalSigAndInitialize(void* rewriterWrapperPtr, FunctionInfo* functionInfo,
                                        ULONG* callTargetStateIndex, ULONG* exceptionIndex,
                                        ULONG* callTargetReturnIndex, ULONG* returnValueIndex,
                                        mdToken* callTargetStateToken, mdToken* exceptionToken,
                                        mdToken* callTargetReturnToken, ILInstr** firstInstruction);

    HRESULT WriteCallTargetReturnGetReturnValue(void* rewriterWrapperPtr, mdTypeSpec callTargetReturnTypeSpec,
                                                ILInstr** instruction);
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_TOKENS_H_
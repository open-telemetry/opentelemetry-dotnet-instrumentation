/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "method_rewriter.h"
#include "calltarget_rewrite_tokens.h"
#include "cor_profiler.h"
#include "il_rewriter_wrapper.h"
#include "integration.h"
#include "logger.h"
#include "stats.h"
#include "util.h"
#include "environment_variables_util.h"

namespace trace
{

namespace
{
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

    std::unique_ptr<CallTargetRewriteTokens> rewriteTokens;

    // *** Get reference to the integration type or trampoline map
    mdTypeRef integration_type_ref = mdTypeRefNil;
    if (use_trampoline)
    {
        hr = CreateTrampolineCallTargetRewriteTokens(module_metadata, integration_definition, &rewriteTokens);
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
    else
    {
        rewriteTokens = CreateDirectCallTargetRewriteTokens(tracerTokens, integration_type_ref,
                                                            ignoreByRefInstrumentation,
                                                            corProfiler->enable_by_ref_instrumentation,
                                                            corProfiler->enable_calltarget_state_by_ref);
    }

    const char* logPrefix = rewriteTokens->OperationName();

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("*** ", logPrefix, "() Start: ", caller->type.name, ".", caller->name,
                      "() [IsVoid=", isVoid, ", IsStatic=", isStatic,
                      ", Trampoline=", rewriteTokens->IsTrampoline(),
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
        original_code = corProfiler->GetILCodes(rewriteTokens->IsTrampoline()
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
    hr = rewriteTokens->ModifyLocalSigAndInitialize(reWriterWrapper, caller, &callTargetStateIndex, &exceptionIndex,
                                                    &callTargetReturnIndex, &returnValueIndex, &callTargetStateToken,
                                                    &exceptionToken, &callTargetReturnToken, &firstInstruction);
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
                if (rewriteTokens->ShouldPassFastArgumentByRef())
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
            reWriterWrapper.CreateArray(rewriteTokens->GetObjectType(), numArgs);
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
                    const auto& tok = methodArguments[i].GetTypeTok(metaEmit, rewriteTokens->GetCorLibAssemblyRef());
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
    hr = rewriteTokens->WriteBeginMethod(reWriterWrapper, &caller->type, methodArguments, &beginCallInstruction);
    if (FAILED(hr))
    {
        // Error message is written to the log in WriteBeginMethod.
        return S_FALSE;
    }
    reWriterWrapper.StLocal(callTargetStateIndex);
    ILInstr* pStateLeaveToBeginOriginalMethodInstr = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** BeginMethod call catch
    ILInstr* beginMethodCatchFirstInstr = nullptr;
    hr = rewriteTokens->WriteLogException(reWriterWrapper, &caller->type, &beginMethodCatchFirstInstr);
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
    if (rewriteTokens->ShouldPassStateByRef())
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
        hr = rewriteTokens->WriteEndVoidMethod(reWriterWrapper, &caller->type, &endMethodCallInstr);
    }
    else
    {
        hr = rewriteTokens->WriteEndMethod(reWriterWrapper, &caller->type, &retFuncArg, &endMethodCallInstr);
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
        hr = rewriteTokens->WriteCallTargetReturnGetReturnValue(reWriterWrapper, &retFuncArg, callTargetReturnToken,
                                                                &callTargetReturnGetReturnInstr);
        if (FAILED(hr))
        {
            return S_FALSE;
        }
        reWriterWrapper.StLocal(returnValueIndex);
    }

    ILInstr* endMethodTryLeave = reWriterWrapper.CreateInstr(CEE_LEAVE_S);

    // *** EndMethod call catch
    ILInstr* endMethodCatchFirstInstr = nullptr;
    hr = rewriteTokens->WriteLogException(reWriterWrapper, &caller->type, &endMethodCatchFirstInstr);
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
        Logger::Info(corProfiler->GetILCodes(rewriteTokens->IsTrampoline()
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
                 "() [IsVoid=", isVoid, ", IsStatic=", isStatic, ", Trampoline=", rewriteTokens->IsTrampoline(),
                 ", IntegrationType=", integration_definition->integration_type.name, ", Arguments=", numArgs, "]");
    return S_OK;
}

} // namespace trace

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include "method_rewriter.h"

namespace trace
{
class TracerMethodRewriter : public MethodRewriter
{
private:
    ILInstr* CreateFilterForException(ILRewriterWrapper* rewriter, mdTypeRef exceptionTypeRef,
                                     mdTypeRef bubbleUpExceptionTypeRef, mdMemberRef containsCallTargetBubbleUpException,
                                     ULONG exceptionValueIndex);

public:
    explicit TracerMethodRewriter(CorProfiler* corProfiler) : MethodRewriter(corProfiler)
    {
    }

    HRESULT Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler,
                    ICorProfilerFunctionControl* pFunctionControl, ICorProfilerInfo* corProfilerInfo) override;
};

} // namespace trace

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_METHOD_REWRITER_H_
#define OTEL_CLR_PROFILER_METHOD_REWRITER_H_

#include "util.h"
#include "cor.h"

struct ILInstr;
class ILRewriterWrapper;

namespace trace
{
    // forward declarations
    class RejitHandlerModule;
    class RejitHandlerModuleMethod;
    class CorProfiler;

class MethodRewriter
{
protected:
    CorProfiler* m_corProfiler;

public:
    explicit MethodRewriter(CorProfiler* corProfiler) : m_corProfiler(corProfiler)
    {
    }

    virtual HRESULT Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler) = 0;

    virtual ~MethodRewriter() = default;
};


class TracerMethodRewriter : public MethodRewriter
{
private:
    static ILInstr* CreateFilterForException(ILRewriterWrapper* rewriter, mdTypeRef exceptionTypeRef,
                                             mdTypeRef bubbleUpExceptionTypeRef, ULONG exceptionValueIndex);

public:
    explicit TracerMethodRewriter(CorProfiler* corProfiler) : MethodRewriter(corProfiler)
    {
    }

    HRESULT Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler) override;
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_METHOD_REWRITER_H_

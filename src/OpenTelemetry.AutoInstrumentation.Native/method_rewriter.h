/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_METHOD_REWRITER_H_
#define OTEL_CLR_PROFILER_METHOD_REWRITER_H_

#include "util.h"
#include "cor.h"

namespace trace
{
    // forward declarations
    class RejitHandlerModule;
    class RejitHandlerModuleMethod;

class MethodRewriter
{
public:
    virtual HRESULT Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler) = 0;
};


class TracerMethodRewriter : public MethodRewriter, public Singleton<TracerMethodRewriter>
{
    friend class Singleton<TracerMethodRewriter>;
    
private:
    TracerMethodRewriter(){}

public:
    HRESULT Rewrite(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler) override;
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_METHOD_REWRITER_H_
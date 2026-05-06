/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_
#define OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

#include "integration.h"
#include "tracer_tokens.h"

namespace trace
{

class CallTargetTrampolineTokens final : public TracerTokens
{
private:
    IntegrationDefinition* integrationDefinition;
    mdTypeRef indexerTypeRef = mdTypeRefNil;
    mdTypeSpec integrationTypeSpec = mdTypeSpecNil;

public:
    CallTargetTrampolineTokens(ModuleMetadata* moduleMetadata, IntegrationDefinition* integrationDefinition);

    HRESULT Initialize();
    mdTypeRef GetIntegrationTypeRef() const;

protected:
    HRESULT EnsureBaseCalltargetTokens() override;

public:
    bool ShouldLoadArgumentsByRef(const bool ignoreByRefInstrumentation) override;
    bool ShouldLoadCallTargetStateByRef() override;
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_REWRITE_TOKENS_H_

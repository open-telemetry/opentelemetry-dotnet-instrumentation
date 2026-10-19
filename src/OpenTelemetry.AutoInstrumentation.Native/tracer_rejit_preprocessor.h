// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include "rejit_preprocessor.h"
#include "tracer_integration_definition.h"

namespace trace
{
class TracerRejitPreprocessor : public RejitPreprocessor<IntegrationDefinition>
{
public:
    TracerRejitPreprocessor(CorProfiler* corProfiler, std::shared_ptr<RejitHandler> handler, std::shared_ptr<RejitWorkOffloader> offloader)
        : RejitPreprocessor(corProfiler, std::move(handler), std::move(offloader), RejitterPriority::Normal) {}

protected:
    const MethodReference& GetTargetMethod(const IntegrationDefinition& integrationDefinition) final;
    const bool GetIsDerived(const IntegrationDefinition& definition) final;
    const bool GetIsInterface(const IntegrationDefinition& definition) final;
    const bool GetIsExactSignatureMatch(const IntegrationDefinition& definition) final;
    const bool GetIsEnabled(const IntegrationDefinition& definition) final;
    void UpdateMethod(RejitHandlerModuleMethod* method, const IntegrationDefinition& definition) final;
    const std::unique_ptr<RejitHandlerModuleMethod>
    CreateMethod(mdMethodDef methodDef, RejitHandlerModule* module, const FunctionInfo& functionInfo,
                 const IntegrationDefinition& integrationDefinition) final;
};

} // namespace trace

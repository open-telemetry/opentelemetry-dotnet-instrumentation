// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include "rejit_handler.h"
#include "tracer_integration_definition.h"

namespace trace
{
class TracerRejitHandlerModuleMethod : public RejitHandlerModuleMethod
{
private:
    std::unique_ptr<IntegrationDefinition> m_integrationDefinition;

public:
    TracerRejitHandlerModuleMethod(
                    mdMethodDef methodDef,
                    RejitHandlerModule* module,
                    const FunctionInfo& functionInfo,
                    const IntegrationDefinition& integrationDefinition,
                    std::unique_ptr<MethodRewriter> methodRewriter);

    IntegrationDefinition* GetIntegrationDefinition();
};
} // namespace trace

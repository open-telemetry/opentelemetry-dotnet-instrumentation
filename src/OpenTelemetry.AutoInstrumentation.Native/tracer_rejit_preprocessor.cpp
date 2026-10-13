// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "tracer_rejit_preprocessor.h"
#include "tracer_handler_module_method.h"
#include "tracer_method_rewriter.h"

namespace trace
{
// TraceIntegrationRejitPreprocessor

const MethodReference& TracerRejitPreprocessor::GetTargetMethod(const IntegrationDefinition& integrationDefinition)
{
    return integrationDefinition.target_method;
}

const bool TracerRejitPreprocessor::GetIsDerived(const IntegrationDefinition& integrationDefinition)
{
    return integrationDefinition.is_derived;
}

const bool TracerRejitPreprocessor::GetIsInterface(const IntegrationDefinition& integrationDefinition)
{
    return integrationDefinition.is_interface;
}

const bool TracerRejitPreprocessor::GetIsExactSignatureMatch(const IntegrationDefinition& integrationDefinition)
{
    return integrationDefinition.is_exact_signature_match;
}

const bool TracerRejitPreprocessor::GetIsEnabled(const IntegrationDefinition& integrationDefinition)
{
    return integrationDefinition.GetEnabled();
}

const std::unique_ptr<RejitHandlerModuleMethod> TracerRejitPreprocessor::CreateMethod(
    const mdMethodDef            methodDef,
    RejitHandlerModule*          module,
    const FunctionInfo&          functionInfo,
    const IntegrationDefinition& integrationDefinition)
{
    return std::make_unique<TracerRejitHandlerModuleMethod>(methodDef, module, functionInfo, integrationDefinition,
                                                            std::make_unique<TracerMethodRewriter>(m_corProfiler));
}

void TracerRejitPreprocessor::UpdateMethod(RejitHandlerModuleMethod* method, const IntegrationDefinition& definition)
{
    auto tracerMethodHandler = static_cast<TracerRejitHandlerModuleMethod*>(method);
    tracerMethodHandler->GetIntegrationDefinition()->Update(definition);
}

} // namespace trace

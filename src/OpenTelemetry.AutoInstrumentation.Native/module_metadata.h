/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_MODULE_METADATA_H_
#define OTEL_CLR_PROFILER_MODULE_METADATA_H_

#include <corhlpr.h>
#include <mutex>
#include <unordered_map>
#include <unordered_set>

#include "calltarget_tokens.h"
#include "clr_helpers.h"
#include "com_ptr.h"
#include "integration.h"
#include "string.h"
#include "tracer_tokens.h"

namespace trace
{

class ModuleMetadata
{
private:
    std::mutex wrapper_mutex;
    std::unique_ptr<std::unordered_map<WSTRING, mdTypeRef>> integration_types = nullptr;
    std::unique_ptr<TracerTokens> tracerTokens = nullptr;
    std::unique_ptr<std::vector<IntegrationDefinition>> integrations = nullptr;

public:
    const ComPtr<IMetaDataImport2> metadata_import{};
    const ComPtr<IMetaDataEmit2> metadata_emit{};
    const ComPtr<IMetaDataAssemblyImport> assembly_import{};
    const ComPtr<IMetaDataAssemblyEmit> assembly_emit{};
    const WSTRING assemblyName = EmptyWStr;
    const AppDomainID app_domain_id;
    const GUID module_version_id;
    const AssemblyProperty* corAssemblyProperty = nullptr;

    ModuleMetadata(ComPtr<IMetaDataImport2> metadata_import, ComPtr<IMetaDataEmit2> metadata_emit,
                   ComPtr<IMetaDataAssemblyImport> assembly_import, ComPtr<IMetaDataAssemblyEmit> assembly_emit,
                   const WSTRING& assembly_name, const AppDomainID app_domain_id, const GUID module_version_id,
                   std::unique_ptr<std::vector<IntegrationDefinition>>&& integrations,
                   const AssemblyProperty* corAssemblyProperty) :
        metadata_import(metadata_import),
        metadata_emit(metadata_emit),
        assembly_import(assembly_import),
        assembly_emit(assembly_emit),
        assemblyName(assembly_name),
        app_domain_id(app_domain_id),
        module_version_id(module_version_id),
        integrations(std::move(integrations)),
        corAssemblyProperty(corAssemblyProperty)
    {
    }

    ModuleMetadata(ComPtr<IMetaDataImport2> metadata_import, ComPtr<IMetaDataEmit2> metadata_emit,
                   ComPtr<IMetaDataAssemblyImport> assembly_import, ComPtr<IMetaDataAssemblyEmit> assembly_emit,
                   const WSTRING& assembly_name, const AppDomainID app_domain_id,
                   const AssemblyProperty* corAssemblyProperty) :
        metadata_import(metadata_import),
        metadata_emit(metadata_emit),
        assembly_import(assembly_import),
        assembly_emit(assembly_emit),
        assemblyName(assembly_name),
        app_domain_id(app_domain_id),
        module_version_id(),
        corAssemblyProperty(corAssemblyProperty)
    {
    }

    bool TryGetIntegrationTypeRef(const WSTRING& keyIn, mdTypeRef& valueOut) const
    {
        if (integration_types == nullptr)
        {
            return false;
        }

        const auto search = integration_types->find(keyIn);

        if (search != integration_types->end())
        {
            valueOut = search->second;
            return true;
        }

        return false;
    }

    void SetIntegrationTypeRef(const WSTRING& keyIn, const mdTypeRef valueIn)
    {
        std::scoped_lock<std::mutex> lock(wrapper_mutex);
        if (integration_types == nullptr)
        {
            integration_types = std::make_unique<std::unordered_map<WSTRING, mdTypeRef>>();
        }

        (*integration_types)[keyIn] = valueIn;
    }

    TracerTokens* GetTracerTokens()
    {
        if (tracerTokens == nullptr)
        {
            tracerTokens =
                std::make_unique<TracerTokens>(this);
        }
        return tracerTokens.get();
    }
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_MODULE_METADATA_H_

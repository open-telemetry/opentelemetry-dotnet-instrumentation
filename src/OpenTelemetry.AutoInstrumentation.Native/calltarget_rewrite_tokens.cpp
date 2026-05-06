/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "calltarget_rewrite_tokens.h"

#include <vector>

#include "cor_profiler.h"
#include "logger.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"

namespace trace
{

const WSTRING calltarget_trampoline_type_name = WStr("__OTelCallTargetTrampoline__");
const WSTRING calltarget_trampoline_indexer_type_name = WStr("__OTelCallTargetIndexer`1");
const WSTRING calltarget_trampoline_state_type_name = WStr("__OTelCallTargetState__");
const WSTRING calltarget_trampoline_return_type_name = WStr("__OTelCallTargetReturn__");
const WSTRING calltarget_trampoline_return_generic_type_name = WStr("__OTelCallTargetReturn__`1");

namespace
{

mdAssemblyRef FindExistingCorLibRef(ModuleMetadata& moduleMetadata)
{
    if (moduleMetadata.assemblyName == mscorlib_assemblyName ||
        moduleMetadata.assemblyName == system_private_corelib_assemblyName)
    {
        return mdTokenNil;
    }

    for (mdAssemblyRef assemblyRef : EnumAssemblyRefs(moduleMetadata.assembly_import))
    {
        auto assemblyMetadata = GetReferencedAssemblyMetadata(moduleMetadata.assembly_import, assemblyRef);
        if (assemblyMetadata.name == mscorlib_assemblyName ||
            assemblyMetadata.name == system_private_corelib_assemblyName)
        {
            return assemblyRef;
        }
    }

    return mdAssemblyRefNil;
}

void AppendCompressedData(std::vector<COR_SIGNATURE>& signature, ULONG value)
{
    COR_SIGNATURE buffer[sizeof(ULONG)];
    ULONG         size = CorSigCompressData(value, buffer);
    signature.insert(signature.end(), buffer, buffer + size);
}

void AppendCompressedToken(std::vector<COR_SIGNATURE>& signature, mdToken token)
{
    COR_SIGNATURE buffer[sizeof(mdToken)];
    ULONG         size = CorSigCompressToken(token, buffer);
    signature.insert(signature.end(), buffer, buffer + size);
}

void AppendTypeToken(std::vector<COR_SIGNATURE>& signature, mdToken token, bool isValueType)
{
    signature.push_back(isValueType ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS);
    AppendCompressedToken(signature, token);
}

HRESULT BuildIndexerMapTypeSpec(ModuleMetadata& moduleMetadata,
                                mdTypeRef       indexerTypeRef,
                                int             integrationIndex,
                                mdTypeSpec*     integrationTypeSpec)
{
    std::vector<COR_SIGNATURE> current{ELEMENT_TYPE_OBJECT};
    for (int i = 0; i <= integrationIndex; i++)
    {
        std::vector<COR_SIGNATURE> next;
        next.push_back(ELEMENT_TYPE_GENERICINST);
        AppendTypeToken(next, indexerTypeRef, false);
        AppendCompressedData(next, 1);
        next.insert(next.end(), current.begin(), current.end());
        current = next;
    }

    return moduleMetadata.metadata_emit->GetTokenFromTypeSpec(current.data(), static_cast<ULONG>(current.size()),
                                                              integrationTypeSpec);
}

} // namespace

CallTargetTrampolineTokens::CallTargetTrampolineTokens(ModuleMetadata*         moduleMetadata,
                                                       IntegrationDefinition* integrationDefinitionPtr) :
    TracerTokens(moduleMetadata), integrationDefinition(integrationDefinitionPtr)
{
}

HRESULT CallTargetTrampolineTokens::Initialize()
{
    if (integrationDefinition == nullptr)
    {
        return E_FAIL;
    }

    if (integrationTypeSpec != mdTypeSpecNil)
    {
        return S_OK;
    }

    auto moduleMetadata = GetMetadata();
    corLibAssemblyRef = FindExistingCorLibRef(*moduleMetadata);
    if (corLibAssemblyRef == mdAssemblyRefNil &&
        moduleMetadata->assemblyName != mscorlib_assemblyName &&
        moduleMetadata->assemblyName != system_private_corelib_assemblyName)
    {
        Logger::Warn("CallTargetTrampolineTokens: skipping module without existing corlib AssemblyRef: ",
                     moduleMetadata->assemblyName);
        return E_FAIL;
    }

    MemberResolver resolver(moduleMetadata->metadata_import, moduleMetadata->metadata_emit);
    HRESULT        hr = S_OK;

    auto get_type = [&](LPCWSTR name, mdToken* token) -> HRESULT {
        hr = resolver.GetTypeRefOrDefByName(corLibAssemblyRef, name, token);
        if (FAILED(hr))
        {
            Logger::Warn("CallTargetTrampolineTokens: failed to resolve type ", WSTRING(name));
        }
        return hr;
    };

    IfFailRet(get_type(SystemObject, &objectTypeRef));
    IfFailRet(get_type(SystemException, &exTypeRef));
    IfFailRet(get_type(calltarget_trampoline_state_type_name.c_str(), &callTargetStateTypeRef));
    IfFailRet(get_type(calltarget_trampoline_return_type_name.c_str(), &callTargetReturnVoidTypeRef));
    IfFailRet(get_type(calltarget_trampoline_return_generic_type_name.c_str(), &callTargetReturnTypeRef));
    IfFailRet(get_type(calltarget_trampoline_indexer_type_name.c_str(), &indexerTypeRef));
    IfFailRet(get_type(calltarget_trampoline_type_name.c_str(), &callTargetTypeRef));

    const int integrationIndex = trace::profiler->GetCallTargetTrampolineIntegrationIndex(*integrationDefinition);
    return BuildIndexerMapTypeSpec(*moduleMetadata, indexerTypeRef, integrationIndex, &integrationTypeSpec);
}

mdTypeRef CallTargetTrampolineTokens::GetIntegrationTypeRef() const
{
    return static_cast<mdTypeRef>(integrationTypeSpec);
}

HRESULT CallTargetTrampolineTokens::EnsureBaseCalltargetTokens()
{
    return Initialize();
}

bool CallTargetTrampolineTokens::ShouldLoadArgumentsByRef(const bool)
{
    return true;
}

bool CallTargetTrampolineTokens::ShouldLoadCallTargetStateByRef()
{
    return true;
}

} // namespace trace

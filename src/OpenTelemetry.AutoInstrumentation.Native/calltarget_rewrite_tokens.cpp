/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "calltarget_rewrite_tokens.h"

#include "cor_profiler.h"
#include "logger.h"
#include "member_resolver.h"
#include "otel_profiler_constants.h"
#include "signature_builder.h"
#include "util.h"

namespace trace
{

const WSTRING calltarget_trampoline_type_name                = WStr("__OTelCallTargetTrampoline__");
const WSTRING calltarget_trampoline_indexer_type_name        = WStr("__OTelCallTargetIndexer`1");
const WSTRING calltarget_trampoline_state_type_name          = WStr("__OTelCallTargetState__");
const WSTRING calltarget_trampoline_return_type_name         = WStr("__OTelCallTargetReturn__");
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

HRESULT BuildIndexerMapTypeSpec(ModuleMetadata& moduleMetadata,
                                mdTypeRef       indexerTypeRef,
                                mdToken         objectTypeRef,
                                int             integrationIndex,
                                mdTypeSpec*     integrationTypeSpec)
{
    SignatureBuilder current;
    current.PushRawByte(ELEMENT_TYPE_OBJECT);

    SignatureBuilder::Class indexerType{indexerTypeRef};
    for (int i = 0; i <= integrationIndex; i++)
    {
        SignatureBuilder next;
        next.PushRawByte(ELEMENT_TYPE_GENERICINST).Push(indexerType).PushCompressedData(1).Push(current);
        current = next;
    }

    auto hr = moduleMetadata.metadata_emit->GetTokenFromTypeSpec(current.Head(), current.Size(), integrationTypeSpec);
    if (SUCCEEDED(hr) && Logger::IsDebugEnabled())
    {
        Logger::Debug("BuildIndexerMapTypeSpec: indexerTypeRef=", HexStr(&indexerTypeRef, sizeof(mdTypeRef)),
                      " objectTypeRef=", HexStr(&objectTypeRef, sizeof(mdToken)),
                      " integrationIndex=", integrationIndex,
                      " integrationTypeSpec=", HexStr(integrationTypeSpec, sizeof(mdTypeSpec)),
                      " signature=", HexStr(current.Head(), static_cast<int>(current.Size())));
    }

    return hr;
}

} // namespace

CallTargetTrampolineTokens::CallTargetTrampolineTokens(ModuleMetadata*        moduleMetadata,
                                                       IntegrationDefinition* integrationDefinitionPtr)
    : TracerTokens(moduleMetadata), integrationDefinition(integrationDefinitionPtr)
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
    corLibAssemblyRef   = FindExistingCorLibRef(*moduleMetadata);
    if (corLibAssemblyRef == mdAssemblyRefNil && moduleMetadata->assemblyName != mscorlib_assemblyName &&
        moduleMetadata->assemblyName != system_private_corelib_assemblyName)
    {
        Logger::Warn("CallTargetTrampolineTokens: skipping module without existing corlib AssemblyRef: ",
                     moduleMetadata->assemblyName);
        return E_FAIL;
    }

    MemberResolver resolver(moduleMetadata->metadata_import, moduleMetadata->metadata_emit);
    HRESULT        hr = S_OK;

    auto get_type = [&](LPCWSTR name, mdToken* token) -> HRESULT
    {
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
    hr =
        BuildIndexerMapTypeSpec(*moduleMetadata, indexerTypeRef, objectTypeRef, integrationIndex, &integrationTypeSpec);
    if (FAILED(hr))
    {
        return hr;
    }

    if (Logger::IsDebugEnabled())
    {
        Logger::Debug("CallTargetTrampolineTokens::Initialize: assembly=", moduleMetadata->assemblyName,
                      " corLibAssemblyRef=", HexStr(&corLibAssemblyRef, sizeof(mdAssemblyRef)),
                      " objectTypeRef=", HexStr(&objectTypeRef, sizeof(mdToken)),
                      " exceptionTypeRef=", HexStr(&exTypeRef, sizeof(mdToken)),
                      " stateTypeRef=", HexStr(&callTargetStateTypeRef, sizeof(mdToken)),
                      " returnVoidTypeRef=", HexStr(&callTargetReturnVoidTypeRef, sizeof(mdToken)),
                      " returnGenericTypeRef=", HexStr(&callTargetReturnTypeRef, sizeof(mdToken)),
                      " indexerTypeRef=", HexStr(&indexerTypeRef, sizeof(mdToken)),
                      " trampolineTypeRef=", HexStr(&callTargetTypeRef, sizeof(mdToken)),
                      " integrationIndex=", integrationIndex,
                      " integrationTypeSpec=", HexStr(&integrationTypeSpec, sizeof(mdTypeSpec)));
    }

    return S_OK;
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

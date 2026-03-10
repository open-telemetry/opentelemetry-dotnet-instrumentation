/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_MEMBER_RESOLVER_H_
#define OTEL_CLR_PROFILER_MEMBER_RESOLVER_H_

#include <vector>
#include <corhlpr.h>

#include "com_ptr.h"
#include "module_metadata.h"

namespace trace
{

class MemberResolver
{
private:
    ComPtr<IMetaDataImport2> metadaImport_;
    ComPtr<IMetaDataEmit2>   metadaEmit_;

public:
    MemberResolver(const ComPtr<IMetaDataImport2>& import, const ComPtr<IMetaDataEmit2>& emit);


    /// <summary>
    /// Uses IMetaDataImport::FindTypeDefByName if tkResolutionScope is mdTokenNill, TypeDef or Assembly
    /// Uses IMetaDataEmit::DefineTypeRefByName otherwise (if tkResolutionScope is AssemblyRef or TypeRef)
    /// </summary>
    HRESULT GetTypeRefOrDefByName(mdToken tkResolutionScope, LPCWSTR szName, mdToken* token);

    /// <summary>
    /// Uses IMetaDataImport::FindMember if tkScope is TypeDef
    /// Uses IMetaDataEmit::DefineMemberRef otherwise (if tkScope is TypeRef or TypeSpec)
    /// </summary>
    HRESULT GetMemberRefOrDef(mdToken tkScope, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdToken* token);
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_MEMBER_RESOLVER_H_

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "member_resolver.h"

namespace trace
{

MemberResolver::MemberResolver(const ComPtr<IMetaDataImport2>& import, const ComPtr<IMetaDataEmit2>& emit)
    : metadaImport_(import), metadaEmit_(emit)
{
}

HRESULT MemberResolver::GetTypeRefOrDefByName(mdToken tkResolutionScope, LPCWSTR szName, mdToken* token)
{
    if (TypeFromToken(tkResolutionScope) == mdtAssembly)
    {
        tkResolutionScope = mdTokenNil;
    }

    if (tkResolutionScope == mdTokenNil || TypeFromToken(tkResolutionScope) == mdtTypeDef)
    {
        return metadaImport_->FindTypeDefByName(szName, tkResolutionScope, token);
    }

    return metadaEmit_->DefineTypeRefByName(tkResolutionScope, szName, token);
}

HRESULT MemberResolver::GetMemberRefOrDef(
    mdToken tkScope, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdToken* token)

{
    if (TypeFromToken(tkScope) == mdtTypeDef)
    {
        return metadaImport_->FindMember(tkScope, szName, pvSigBlob, cbSigBlob, token);
    }

    return metadaEmit_->DefineMemberRef(tkScope, szName, pvSigBlob, cbSigBlob, token);
}
} // namespace trace

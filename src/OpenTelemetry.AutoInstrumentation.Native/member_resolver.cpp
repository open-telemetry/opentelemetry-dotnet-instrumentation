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

    // TODO: Prior to metadaEmit_->DefineTypeRefByName it may be good to try
    // return metadaImport_->FindTypeRef(tkResolutionScope, szName, ptr);
    return metadaEmit_->DefineTypeRefByName(tkResolutionScope, szName, token);
}

HRESULT MemberResolver::GetMemberRefOrDef(
    mdToken tkImport, LPCWSTR szName, PCCOR_SIGNATURE pvSigBlob, ULONG cbSigBlob, mdToken* token)

{
    if (TypeFromToken(tkImport) == mdtTypeDef)
    {
        return metadaImport_->FindMember(tkImport, szName, pvSigBlob, cbSigBlob, token);
    }

    // TODO: Prior to metadaEmit_->DefineMemberRef it may be good to try
    // return metadaImport_->FindMemberRef(tkImport, szName, pvSigBlob, cbSigBlob, token);
    return metadaEmit_->DefineMemberRef(tkImport, szName, pvSigBlob, cbSigBlob, token);
}
} // namespace trace

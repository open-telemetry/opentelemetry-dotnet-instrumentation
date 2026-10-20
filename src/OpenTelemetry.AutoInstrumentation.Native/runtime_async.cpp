/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "runtime_async.h"

namespace trace
{

namespace
{
// The body of a runtime-async method declaring Task or ValueTask leaves nothing at `ret`.
// Static storage is required because TypeSignature borrows the signature blob.
constexpr COR_SIGNATURE kVoidReturnSignature[] = {ELEMENT_TYPE_VOID};

bool IsTaskLike(const WSTRING& name, bool isGenericInst, bool isValueTypeShape)
{
    if (isGenericInst)
    {
        return isValueTypeShape ? name == SystemThreadingTasksValueTaskGeneric
                                : name == SystemThreadingTasksTaskGeneric;
    }

    return isValueTypeShape ? name == SystemThreadingTasksValueTask : name == SystemThreadingTasksTask;
}
} // namespace

bool ParseTaskLikeReturnShape(const TypeSignature& declared, mdToken& openTypeToken, bool& isGenericInst,
                              bool& isValueTypeShape, TypeSignature& typeArg)
{
    openTypeToken = mdTokenNil;
    isGenericInst = false;
    isValueTypeShape = false;
    typeArg = {};

    if (declared.pbBase == nullptr || declared.length == 0)
    {
        return false;
    }

    PCCOR_SIGNATURE const start = &declared.pbBase[declared.offset];
    PCCOR_SIGNATURE const end = start + declared.length;
    PCCOR_SIGNATURE current = start;

    unsigned char elementType;
    if (!ParseByte(current, end, &elementType))
    {
        return false;
    }

    if (elementType == ELEMENT_TYPE_GENERICINST)
    {
        unsigned char genericElementType;
        if (!ParseByte(current, end, &genericElementType) ||
            (genericElementType != ELEMENT_TYPE_CLASS && genericElementType != ELEMENT_TYPE_VALUETYPE))
        {
            return false;
        }

        unsigned char tokenType;
        unsigned tokenIndex;
        if (!ParseTypeDefOrRefEncoded(current, end, &tokenType, &tokenIndex))
        {
            return false;
        }

        openTypeToken = TokenFromRid(tokenIndex, CorSigDecodeTokenType(tokenType));

        unsigned genericArgumentCount = 0;
        if (!ParseNumber(current, end, &genericArgumentCount) || genericArgumentCount != 1)
        {
            return false;
        }

        PCCOR_SIGNATURE const typeArgStart = current;
        if (!ParseType(current, end) || current != end)
        {
            return false;
        }

        isGenericInst = true;
        isValueTypeShape = genericElementType == ELEMENT_TYPE_VALUETYPE;
        typeArg = TypeSignature{declared.offset + static_cast<ULONG>(typeArgStart - start),
                                static_cast<ULONG>(current - typeArgStart), declared.pbBase};
        return true;
    }

    if (elementType == ELEMENT_TYPE_CLASS || elementType == ELEMENT_TYPE_VALUETYPE)
    {
        unsigned char tokenType;
        unsigned tokenIndex;
        if (!ParseTypeDefOrRefEncoded(current, end, &tokenType, &tokenIndex) || current != end)
        {
            return false;
        }

        openTypeToken = TokenFromRid(tokenIndex, CorSigDecodeTokenType(tokenType));
        isValueTypeShape = elementType == ELEMENT_TYPE_VALUETYPE;
        return true;
    }

    return false;
}

bool TryGetRuntimeAsyncEffectiveReturnType(const TypeSignature& declared,
                                           const ComPtr<IMetaDataImport2>& metadataImport, TypeSignature& effective)
{
    effective = {};

    mdToken openTypeToken = mdTokenNil;
    bool isGenericInst = false;
    bool isValueTypeShape = false;
    TypeSignature typeArg{};

    if (!ParseTaskLikeReturnShape(declared, openTypeToken, isGenericInst, isValueTypeShape, typeArg))
    {
        return false;
    }

    const auto typeInfo = GetTypeInfo(metadataImport, openTypeToken);
    if (!typeInfo.IsValid() || !IsTaskLike(typeInfo.name, isGenericInst, isValueTypeShape))
    {
        return false;
    }

    effective = isGenericInst
                    ? typeArg
                    : TypeSignature{0, static_cast<ULONG>(sizeof(kVoidReturnSignature)), kVoidReturnSignature};
    return true;
}

} // namespace trace

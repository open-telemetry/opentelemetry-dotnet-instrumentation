// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "signature_builder.h"

namespace trace
{
SignatureBuilder::SignatureBuilder(std::initializer_list<COR_SIGNATURE> bytes) : blob_(bytes) {}

SignatureBuilder& SignatureBuilder::PushRawByte(COR_SIGNATURE byte)
{
    blob_.push_back(byte);
    return *this;
}

SignatureBuilder& SignatureBuilder::PushRawBytes(COR_SIGNATURE const* begin, COR_SIGNATURE const* end)
{
    blob_.insert(blob_.end(), begin, end);
    return *this;
}

SignatureBuilder& SignatureBuilder::PushRawBytes(std::initializer_list<COR_SIGNATURE> bytes)
{
    blob_.insert(blob_.end(), bytes);
    return *this;
}

SignatureBuilder& SignatureBuilder::PushCompressedData(ULONG data)
{
    COR_SIGNATURE compressed[sizeof(ULONG)];
    ULONG         compressedSize = CorSigCompressData(data, compressed);
    for (ULONG i = 0; i < compressedSize; i++)
    {
        PushRawByte(compressed[i]);
    }
    return *this;
}

SignatureBuilder& SignatureBuilder::PushToken(mdToken token)
{
    COR_SIGNATURE compressed[sizeof(mdToken)];
    ULONG         compressedSize = CorSigCompressToken(token, compressed);
    for (ULONG i = 0; i < compressedSize; i++)
    {
        PushRawByte(compressed[i]);
    }
    return *this;
}

PCCOR_SIGNATURE SignatureBuilder::Head() const
{
    return blob_.data();
}

ULONG SignatureBuilder::Size() const
{
    return (ULONG)blob_.size();
}

} // namespace trace

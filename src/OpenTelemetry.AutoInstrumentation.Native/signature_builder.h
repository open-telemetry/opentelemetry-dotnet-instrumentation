/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_
#define OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_

#include <vector>
#include <corhlpr.h>

namespace trace
{

class SignatureBuilder
{
    std::vector<COR_SIGNATURE> blob_;

public:
    SignatureBuilder() = default;
    SignatureBuilder(std::initializer_list<COR_SIGNATURE> bytes);

    SignatureBuilder& PushRawByte(COR_SIGNATURE byte);
    SignatureBuilder& PushRawBytes(COR_SIGNATURE const* begin, COR_SIGNATURE const* end);
    SignatureBuilder& PushRawBytes(std::initializer_list<COR_SIGNATURE> bytes);
    SignatureBuilder& PushCompressedData(ULONG data);
    SignatureBuilder& PushToken(mdToken token);

    PCCOR_SIGNATURE Head() const;
    ULONG Size() const;
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_

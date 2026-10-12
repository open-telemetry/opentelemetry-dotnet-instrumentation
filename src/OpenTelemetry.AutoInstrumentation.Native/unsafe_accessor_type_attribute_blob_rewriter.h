/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_BLOB_REWRITER_H_
#define OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_BLOB_REWRITER_H_

#include <unordered_map>
#include <vector>

#include "clr_helpers.h"

namespace trace
{

// Parses the attribute's string argument and rebuilds the blob because the encoded length can grow or shrink. All
// trailing metadata bytes are preserved. False leaves both outputs empty.
bool TryRewriteUnsafeAccessorTypeAttributeBlob(
    const BYTE*                                              blob,
    ULONG                                                    blob_size,
    std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
    std::vector<BYTE>&                                       rewritten_blob,
    std::vector<WSTRING>&                                    redirected_assembly_names);

} // namespace trace

#endif // OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_BLOB_REWRITER_H_

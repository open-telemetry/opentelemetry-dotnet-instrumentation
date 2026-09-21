/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_
#define OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

#include "clr_helpers.h"

namespace trace
{

class ModuleMetadata;

// Returns whether this metadata scope defines UnsafeAccessorTypeAttribute as a TypeDef. Called for CoreLib as a
// runtime capability check; it does not search for usages.
bool HasUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import);

// Rewrites every mapped assembly qualifier, including qualifiers in nested generic arguments. Missing or lower versions
// are raised; an explicit higher version is preserved and can raise the shared target until an earlier reference fixes
// it. True means rewritten output was produced; false leaves both outputs empty but can still mean that a preserved
// higher version updated shared state. redirected_assembly_names contains one entry per rewritten qualifier and can
// therefore contain duplicate names.
bool TryRewriteUnsafeAccessorTypeName(std::string_view                                         type_name,
                                      std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                      std::string&                                             rewritten_type_name,
                                      std::vector<WSTRING>& redirected_assembly_names);

// Parses the attribute's string argument and rebuilds the blob because the encoded length can grow or shrink. All
// trailing metadata bytes are preserved. False leaves both outputs empty.
bool TryRewriteUnsafeAccessorTypeAttributeBlob(
    const BYTE*                                              blob,
    ULONG                                                    blob_size,
    std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
    std::vector<BYTE>&                                       rewritten_blob,
    std::vector<WSTRING>&                                    redirected_assembly_names);

// Scans one module for UnsafeAccessorType attributes on UnsafeAccessor return values and parameters. Rewritten
// qualifier state is committed only after a successful metadata write.
void UpdateUnsafeAccessorTypeAttributes(const ModuleMetadata&                                    module_metadata,
                                        std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects);

} // namespace trace

#endif // OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

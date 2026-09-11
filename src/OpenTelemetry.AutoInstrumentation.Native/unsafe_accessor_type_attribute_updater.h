/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_
#define OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

#include <string>
#include <unordered_map>
#include <vector>

#include "clr_helpers.h"

namespace trace
{

class ModuleMetadata;

// Returns whether this metadata scope defines UnsafeAccessorTypeAttribute as a TypeDef. Called for CoreLib as a
// runtime capability check; it does not search for usages.
bool HasUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import);

// Rewrites only the outer assembly qualifier: adds Version when absent or replaces a lower version. An explicit higher
// version is preserved and can raise the shared target until a redirect is committed. True means rewritten output was
// produced; false can still mean that a preserved higher version updated the shared redirection state.
bool TryRewriteUnsafeAccessorTypeName(const std::string&                                       type_name,
                                      std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                      std::string&                                             rewritten_type_name,
                                      WSTRING& redirected_assembly_name);

// Parses the attribute's string argument and rebuilds the blob because the encoded length can grow or shrink. All
// trailing metadata bytes are preserved. Outputs are valid only when this function returns true.
bool TryRewriteUnsafeAccessorTypeAttributeBlob(
    const BYTE*                                              blob,
    ULONG                                                    blob_size,
    std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
    std::vector<BYTE>&                                       rewritten_blob,
    WSTRING&                                                 redirected_assembly_name);

// Scans one module for UnsafeAccessorType attributes on UnsafeAccessor return values and parameters. Redirect state is
// committed only after a successful metadata write.
void UpdateUnsafeAccessorTypeAttributes(const ModuleMetadata&                                    module_metadata,
                                        std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects);

} // namespace trace

#endif // OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

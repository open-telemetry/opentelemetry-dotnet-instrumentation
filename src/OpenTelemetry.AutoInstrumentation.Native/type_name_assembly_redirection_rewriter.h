/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REDIRECTION_REWRITER_H_
#define OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REDIRECTION_REWRITER_H_

#include <string>
#include <string_view>
#include <unordered_map>
#include <vector>

#include "clr_helpers.h"

namespace trace
{

// Rewrites every mapped assembly qualifier, including qualifiers in nested generic arguments. Missing or lower versions
// are raised to the configured target; equal or higher versions are preserved. True means rewritten output was
// produced; false leaves both outputs empty. redirected_assembly_names contains one entry per rewritten qualifier and
// can therefore contain duplicate names.
bool TryRewriteTypeNameAssemblyRedirections(std::string_view                                         type_name,
                                            std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                            std::string&          rewritten_type_name,
                                            std::vector<WSTRING>& redirected_assembly_names);

} // namespace trace

#endif // OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REDIRECTION_REWRITER_H_

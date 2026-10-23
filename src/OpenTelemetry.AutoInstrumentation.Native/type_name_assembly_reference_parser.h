/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REFERENCE_PARSER_H_
#define OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REFERENCE_PARSER_H_

#include <cstddef>
#include <optional>
#include <string>
#include <string_view>
#include <vector>

namespace trace
{

struct Span
{
    size_t begin;
    size_t end;

    bool Empty() const noexcept
    {
        return begin >= end;
    }

    std::string_view View(std::string_view value) const
    {
        return value.substr(begin, end - begin);
    }

    static Span Trimmed(std::string_view value, Span span);
};

struct ParsedAssemblyReference
{
    std::string         assembly_name;
    size_t              insert_version_at;
    std::optional<Span> version_span;
};

// Finds assembly qualifications in a reflection type name, including those inside generic arguments. The assembly
// names are unescaped; spans and insertion positions refer to the original type name. False means malformed syntax.
bool ParseTypeNameAssemblyReferences(std::string_view type_name, std::vector<ParsedAssemblyReference>& references);

} // namespace trace

#endif // OTEL_CLR_PROFILER_TYPE_NAME_ASSEMBLY_REFERENCE_PARSER_H_

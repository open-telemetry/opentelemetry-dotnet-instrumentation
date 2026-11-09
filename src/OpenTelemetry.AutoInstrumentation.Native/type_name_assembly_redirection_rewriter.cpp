/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "type_name_assembly_redirection_rewriter.h"

#include <algorithm>
#include <charconv>
#include <limits>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#include "ascii_string_utils.h"
#include "logger.h"
#include "string_utils.h"
#include "type_name_assembly_reference_parser.h"

namespace trace
{

namespace
{

constexpr USHORT unspecified_version_component = (std::numeric_limits<USHORT>::max)();

bool TryParseVersion(const std::string_view value, ASSEMBLYMETADATA& version)
{
    // Assembly display names accept two to four version components. Missing build and revision components are zero
    // when compared with the four-part versions in the redirection map.
    USHORT     parsed[4]{};
    size_t     component_count           = 0;
    bool       fully_consumed            = false;
    auto       current                   = value.data();
    const auto end                       = current + value.size();
    bool       has_unspecified_component = false;
    for (size_t index = 0; index < 4; index++)
    {
        const auto   separator        = std::find(current, end, '.');
        unsigned int parsed_component = 0;
        const auto   result           = std::from_chars(current, separator, parsed_component);
        if (current == separator || result.ec != std::errc{} || result.ptr != separator ||
            parsed_component > unspecified_version_component)
        {
            return false;
        }

        const auto component = static_cast<USHORT>(parsed_component);
        // The runtime accepts 65535 only for build and revision, where it means unspecified. Normalize it and every
        // following component to zero so comparison follows the runtime's assembly display-name semantics.
        if (component == unspecified_version_component)
        {
            if (index < 2)
            {
                return false;
            }
            has_unspecified_component = true;
        }
        parsed[index] = has_unspecified_component ? 0 : component;
        component_count++;
        if (separator == end)
        {
            fully_consumed = true;
            break;
        }
        current = separator + 1;
    }

    if (component_count < 2 || !fully_consumed)
    {
        return false;
    }

    version.usMajorVersion   = parsed[0];
    version.usMinorVersion   = parsed[1];
    version.usBuildNumber    = parsed[2];
    version.usRevisionNumber = parsed[3];
    return true;
}

} // namespace

bool TryRewriteTypeNameAssemblyRedirections(const std::string_view                                   type_name,
                                            std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                            std::string&          rewritten_type_name,
                                            std::vector<WSTRING>& redirected_assembly_names)
{
    rewritten_type_name.clear();
    redirected_assembly_names.clear();

    std::vector<ParsedAssemblyReference> references;
    Logger::Debug("TypeNameAssemblyRedirectionRewriter: parsing type name '", type_name, "'.");
    if (!ParseTypeNameAssemblyReferences(type_name, references))
    {
        Logger::Debug("TypeNameAssemblyRedirectionRewriter: failed to parse type name.");
        return false;
    }
    Logger::Debug("TypeNameAssemblyRedirectionRewriter: parsed ", references.size(),
                  " assembly reference(s) from type name '", type_name, "'.");

    struct Edit
    {
        size_t      begin;
        size_t      end;
        std::string value;
        WSTRING     assembly_name;
    };
    std::vector<Edit> edits;
    for (const auto& reference : references)
    {
        const auto version =
            reference.version_span ? reference.version_span->View(type_name) : std::string_view{"<missing>"};
        Logger::Debug("TypeNameAssemblyRedirectionRewriter: parsed assembly reference '", reference.assembly_name,
                      "', Version='", version, "'.");
        const auto assembly_name = ToWSTRING(reference.assembly_name);
        auto       redirect      = assembly_redirects.find(assembly_name);
        if (redirect == assembly_redirects.end())
        {
            redirect = std::find_if(assembly_redirects.begin(), assembly_redirects.end(),
                                    [&assembly_name](const auto& entry)
                                    { return EqualsIgnoreAsciiCase<WCHAR>(entry.first, assembly_name); });
        }
        if (redirect == assembly_redirects.end())
        {
            Logger::Debug("TypeNameAssemblyRedirectionRewriter: no redirect is configured for assembly '",
                          reference.assembly_name, "'.");
            continue;
        }

        ASSEMBLYMETADATA parsed_version{};
        if (reference.version_span && !TryParseVersion(reference.version_span->View(type_name), parsed_version))
        {
            Logger::Warn("TypeNameAssemblyRedirectionRewriter: cannot redirect assembly '", reference.assembly_name,
                         "' because Version='", version,
                         "' is invalid. Enable Debug logging for the attribute source and parsed type-name details.");
            return false;
        }
        if (reference.version_span && redirect->second.CompareToAssemblyVersion(parsed_version) <= 0)
        {
            // Preserve equal or higher requests. In particular, do not redirect a higher application request
            // downward to the profiler's configured version.
            Logger::Debug("TypeNameAssemblyRedirectionRewriter: preserving assembly '", reference.assembly_name,
                          "' with Version='", version, "'.");
            continue;
        }

        const auto target_version = ToString(redirect->second.VersionStr());
        Logger::Debug("TypeNameAssemblyRedirectionRewriter: rewriting assembly '", reference.assembly_name,
                      "' from Version='", version, "' to Version='", target_version, "'.");
        if (!reference.version_span)
        {
            // An explicit version prevents a lower TPA copy from binding before the managed resolver can
            // participate.
            edits.push_back({reference.insert_version_at, reference.insert_version_at, ", Version=" + target_version,
                             redirect->first});
        }
        else
        {
            edits.push_back(
                {reference.version_span->begin, reference.version_span->end, target_version, redirect->first});
        }
    }

    if (edits.empty())
    {
        return false;
    }

    std::sort(edits.begin(), edits.end(),
              [](const Edit& left, const Edit& right)
              { return left.begin != right.begin ? left.begin < right.begin : left.end < right.end; });
    std::string          rewritten;
    std::vector<WSTRING> rewritten_assembly_names;
    rewritten.reserve(type_name.size());
    auto copied = size_t{0};
    for (const auto& edit : edits)
    {
        if (edit.begin < copied || edit.end < edit.begin || edit.end > type_name.size())
        {
            return false;
        }

        rewritten.append(type_name.data() + copied, edit.begin - copied);
        rewritten.append(edit.value);
        copied = edit.end;
        rewritten_assembly_names.push_back(edit.assembly_name);
    }
    rewritten.append(type_name.data() + copied, type_name.size() - copied);
    rewritten_type_name       = std::move(rewritten);
    redirected_assembly_names = std::move(rewritten_assembly_names);
    Logger::Debug("TypeNameAssemblyRedirectionRewriter: rewrote ", redirected_assembly_names.size(),
                  " assembly reference(s) in type name '", type_name, "'.");
    return true;
}

} // namespace trace

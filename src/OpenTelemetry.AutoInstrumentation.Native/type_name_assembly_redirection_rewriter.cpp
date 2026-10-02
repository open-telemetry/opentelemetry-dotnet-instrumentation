/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "type_name_assembly_redirection_rewriter.h"

#include <algorithm>
#include <charconv>
#include <limits>
#include <optional>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#include "logger.h"
#include "string_utils.h"

namespace trace
{

namespace
{

// A SerString contains UTF-8 bytes. The reflection type-name delimiters inspected below are all ASCII, while every
// non-ASCII UTF-8 byte has its high bit set, so byte-wise syntax parsing is safe. Keeping whitespace and case handling
// ASCII-only also makes it independent of the host process locale.
constexpr bool IsAsciiSpace(const char value) noexcept
{
    return value == ' ' || value == '\t' || value == '\n' || value == '\v' || value == '\f' || value == '\r';
}

template <typename T>
constexpr T ToAsciiLower(const T value) noexcept
{
    return value >= static_cast<T>('A') && value <= static_cast<T>('Z')
               ? static_cast<T>(value - static_cast<T>('A') + static_cast<T>('a'))
               : value;
}

bool EqualsIgnoreCase(const std::string_view left, const std::string_view right)
{
    return left.size() == right.size() && std::equal(left.begin(), left.end(), right.begin(),
                                                     [](const char left_char, const char right_char)
                                                     { return ToAsciiLower(left_char) == ToAsciiLower(right_char); });
}

bool EqualsIgnoreCase(const WSTRING& left, const WSTRING& right)
{
    return left.size() == right.size() && std::equal(left.begin(), left.end(), right.begin(),
                                                     [](const WCHAR left_char, const WCHAR right_char)
                                                     { return ToAsciiLower(left_char) == ToAsciiLower(right_char); });
}

struct Span
{
    size_t begin;
    size_t end;

    bool Empty() const noexcept
    {
        return begin >= end;
    }

    std::string_view View(const std::string_view value) const
    {
        return value.substr(begin, end - begin);
    }
};

Span TrimmedSpan(const std::string_view value, Span span)
{
    while (!span.Empty() && IsAsciiSpace(value[span.begin]))
    {
        span.begin++;
    }
    while (!span.Empty() && IsAsciiSpace(value[span.end - 1]))
    {
        span.end--;
    }
    return span;
}

bool TryFindUnescaped(const std::string_view value,
                      const Span             span,
                      const char             expected,
                      std::optional<size_t>& position)
{
    position.reset();
    auto current_position = span.begin;
    while (current_position < span.end)
    {
        switch (value[current_position])
        {
            case '\\':
                if (current_position + 1 == span.end)
                {
                    return false;
                }
                current_position += 2;
                break;
            case '[':
            case ']':
                // Brackets in an assembly qualification must be escaped.
                return false;
            default:
                if (value[current_position] == expected)
                {
                    position = current_position;
                    return true;
                }
                current_position++;
                break;
        }
    }
    return true;
}

bool TryFindTopLevelSeparator(const std::string_view type_name,
                              const Span             span,
                              const char             separator,
                              std::optional<size_t>& position)
{
    position.reset();
    size_t bracket_depth    = 0;
    auto   current_position = span.begin;
    while (current_position < span.end)
    {
        const auto current = type_name[current_position];
        switch (current)
        {
            case '\\':
                if (current_position + 1 == span.end)
                {
                    return false;
                }
                current_position += 2;
                continue;
            case '[':
                bracket_depth++;
                break;
            case ']':
                if (bracket_depth == 0)
                {
                    return false;
                }
                bracket_depth--;
                break;
            default:
                if (current == separator && bracket_depth == 0)
                {
                    position = current_position;
                    return true;
                }
                break;
        }
        current_position++;
    }

    return bracket_depth == 0;
}

std::optional<size_t> FindClosingBracket(const std::string_view type_name,
                                         const size_t           opening_bracket,
                                         const size_t           end)
{
    if (opening_bracket >= end || type_name[opening_bracket] != '[')
    {
        return std::nullopt;
    }

    size_t depth    = 1;
    auto   position = opening_bracket + 1;
    while (position < end)
    {
        switch (type_name[position])
        {
            case '\\':
                if (position + 1 == end)
                {
                    return std::nullopt;
                }
                position += 2;
                continue;
            case '[':
                depth++;
                break;
            case ']':
                if (--depth == 0)
                {
                    return position;
                }
                break;
            default:
                break;
        }
        position++;
    }

    return std::nullopt;
}

bool TryGetNextQualifierPart(const std::string_view value, Span& remaining, Span& part)
{
    std::optional<size_t> separator_position;
    if (!TryFindUnescaped(value, remaining, ',', separator_position))
    {
        return false;
    }
    part            = {remaining.begin, separator_position.value_or(remaining.end)};
    remaining.begin = separator_position ? *separator_position + 1 : remaining.end;
    part            = TrimmedSpan(value, part);
    return true;
}

bool TryParseVersion(const std::string_view value, ASSEMBLYMETADATA& version)
{
    // Assembly display names accept two to four version components. Missing build and revision components are zero
    // when compared with the four-part versions in the redirection map.
    USHORT     parsed[4]{};
    size_t     component_count = 0;
    bool       fully_consumed  = false;
    auto       current         = value.data();
    const auto end             = current + value.size();
    for (size_t index = 0; index < 4; index++)
    {
        const auto   separator        = std::find(current, end, '.');
        unsigned int parsed_component = 0;
        const auto   result           = std::from_chars(current, separator, parsed_component);
        if (current == separator || result.ec != std::errc{} || result.ptr != separator ||
            parsed_component > (std::numeric_limits<USHORT>::max)())
        {
            return false;
        }

        parsed[index] = static_cast<USHORT>(parsed_component);
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

struct ParsedAssemblyReference
{
    Span                assembly_name;
    size_t              insert_version_at;
    std::optional<Span> version_span;
    ASSEMBLYMETADATA    parsed_version{};
};

// Reflection type names come from module metadata. Bound recursive generic parsing so malformed metadata cannot
// exhaust the profiler's native stack; 64 is well above practical generic nesting.
constexpr size_t max_generic_nesting_depth = 64;

// ParseType and ParseTypeArguments call each other, so declare ParseType before defining ParseTypeArguments.
bool ParseType(std::string_view                      type_name,
               Span                                  type,
               size_t                                generic_nesting_depth,
               std::vector<ParsedAssemblyReference>& references);

// For Outer`2[[First, First.Assembly],Second], the arguments span covers "[First, First.Assembly],Second". ParseType
// recursively records First.Assembly; Second is unqualified, so there is no assembly to record for it.
bool ParseTypeArguments(const std::string_view                type_name,
                        Span                                  arguments,
                        const size_t                          generic_nesting_depth,
                        std::vector<ParsedAssemblyReference>& references)
{
    while (!arguments.Empty())
    {
        std::optional<size_t> separator;
        if (!TryFindTopLevelSeparator(type_name, arguments, ',', separator))
        {
            return false;
        }
        Span argument{arguments.begin, separator.value_or(arguments.end)};
        arguments.begin = separator ? *separator + 1 : arguments.end;
        argument        = TrimmedSpan(type_name, argument);
        if (argument.Empty())
        {
            continue;
        }

        // Assembly-qualified generic arguments have their own brackets. Decide per argument so mixed lists such as
        // Outer`2[[First, First.Assembly],Second] are parsed correctly.
        if (type_name[argument.begin] == '[')
        {
            const auto closing_bracket = FindClosingBracket(type_name, argument.begin, argument.end);
            if (!closing_bracket || *closing_bracket != argument.end - 1)
            {
                return false;
            }
            argument = {argument.begin + 1, *closing_bracket};
        }

        if (!ParseType(type_name, argument, generic_nesting_depth, references))
        {
            return false;
        }
    }
    return true;
}

bool ParseAssemblyQualification(const std::string_view                type_name,
                                Span                                  qualification,
                                std::vector<ParsedAssemblyReference>& references)
{
    Span assembly_name;
    if (!TryGetNextQualifierPart(type_name, qualification, assembly_name))
    {
        return false;
    }
    if (assembly_name.Empty())
    {
        return true;
    }

    ParsedAssemblyReference reference{assembly_name, assembly_name.end};
    while (!qualification.Empty())
    {
        Span qualifier;
        if (!TryGetNextQualifierPart(type_name, qualification, qualifier))
        {
            return false;
        }
        std::optional<size_t> equals;
        if (!TryFindUnescaped(type_name, qualifier, '=', equals))
        {
            return false;
        }
        if (!reference.version_span && equals &&
            EqualsIgnoreCase(TrimmedSpan(type_name, {qualifier.begin, *equals}).View(type_name), "Version"))
        {
            const auto value       = TrimmedSpan(type_name, {*equals + 1, qualifier.end});
            reference.version_span = value;
            if (!TryParseVersion(value.View(type_name), reference.parsed_version))
            {
                return false;
            }
        }
    }
    references.push_back(reference);
    return true;
}

// Example:
// Outer`1[[Inner, Inner.Assembly, Version=1.0.0.0]], Outer.Assembly, Version=2.0.0.0
// recursively records Inner.Assembly at 1.0.0.0, then records Outer.Assembly at 2.0.0.0.
bool ParseType(const std::string_view                type_name,
               Span                                  type,
               const size_t                          generic_nesting_depth,
               std::vector<ParsedAssemblyReference>& references)
{
    type = TrimmedSpan(type_name, type);
    if (type.Empty())
    {
        return true;
    }

    // The first top-level comma begins this type's assembly qualification. Only the preceding TypeSpec can contain
    // generic-argument lists, so scan it for brackets and recursively parse each argument before the outer type.
    std::optional<size_t> assembly_separator;
    if (!TryFindTopLevelSeparator(type_name, type, ',', assembly_separator))
    {
        return false;
    }
    const auto type_name_end = assembly_separator.value_or(type.end);
    auto       position      = type.begin;

    while (position < type_name_end)
    {
        switch (type_name[position])
        {
            case '\\':
                if (position + 1 == type_name_end)
                {
                    return false;
                }
                position += 2;
                break;
            case '[':
            {
                // Match the argument list, then split it; ParseTypeArguments calls ParseType for each argument.
                const auto closing_bracket = FindClosingBracket(type_name, position, type_name_end);
                if (!closing_bracket || generic_nesting_depth == max_generic_nesting_depth ||
                    !ParseTypeArguments(type_name, {position + 1, *closing_bracket}, generic_nesting_depth + 1,
                                        references))
                {
                    return false;
                }
                position = *closing_bracket + 1;
                break;
            }
            default:
                position++;
                break;
        }
    }

    if (!assembly_separator)
    {
        // An unqualified type resolves in the current assembly or CoreLib and has no assembly reference to redirect.
        return true;
    }

    return ParseAssemblyQualification(type_name, {*assembly_separator + 1, type.end}, references);
}

std::string Unescape(const std::string_view value)
{
    std::string result;
    result.reserve(value.size());
    auto position = size_t{0};
    while (position < value.size())
    {
        if (value[position] == '\\' && position + 1 < value.size())
        {
            position++;
        }
        result.push_back(value[position++]);
    }
    return result;
}

std::string VersionString(const AssemblyVersionRedirection& redirect)
{
    // Type names contain UTF-8, so format directly instead of creating and converting the WSTRING used by VersionStr.
    return std::to_string(redirect.usMajorVersion) + "." + std::to_string(redirect.usMinorVersion) + "." +
           std::to_string(redirect.usBuildNumber) + "." + std::to_string(redirect.usRevisionNumber);
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
    if (!ParseType(type_name, {0, type_name.size()}, 0, references))
    {
        return false;
    }

    using RedirectIterator = decltype(assembly_redirects.begin());
    struct ResolvedReference
    {
        const ParsedAssemblyReference* reference;
        RedirectIterator               redirect;
    };
    std::vector<ResolvedReference> resolved_references;
    resolved_references.reserve(references.size());
    for (const auto& reference : references)
    {
        const auto assembly_name = ToWSTRING(Unescape(reference.assembly_name.View(type_name)));
        auto       redirect      = assembly_redirects.find(assembly_name);
        if (redirect == assembly_redirects.end())
        {
            redirect = std::find_if(assembly_redirects.begin(), assembly_redirects.end(),
                                    [&assembly_name](const auto& entry)
                                    { return EqualsIgnoreCase(entry.first, assembly_name); });
        }
        if (redirect != assembly_redirects.end())
        {
            resolved_references.push_back({&reference, redirect});
        }
    }

    // First establish the final target for every assembly. Delay committing a raised target until every reference in
    // this attribute has been considered, so repeated references use the highest requested version consistently.
    std::vector<RedirectIterator> raised_redirects;
    for (const auto& resolved_reference : resolved_references)
    {
        const auto& reference = *resolved_reference.reference;
        const auto  redirect  = resolved_reference.redirect;
        if (!reference.version_span)
        {
            continue;
        }

        const auto version_comparison = redirect->second.CompareToAssemblyVersion(reference.parsed_version);
        if (version_comparison >= 0)
        {
            continue;
        }

        // Never lower a requested version. Before any redirect is committed, let this higher request raise the shared
        // target; afterward, changing it could disagree with metadata already rewritten.
        if (redirect->second.ulRedirectionCount == 0)
        {
            Logger::Info("UnsafeAccessorTypeAttributeUpdater: redirection update for [", redirect->first,
                         "] to_version=", AssemblyVersionStr(reference.parsed_version),
                         " previous_version_redirection=", redirect->second.VersionStr());
            redirect->second.usMajorVersion   = reference.parsed_version.usMajorVersion;
            redirect->second.usMinorVersion   = reference.parsed_version.usMinorVersion;
            redirect->second.usBuildNumber    = reference.parsed_version.usBuildNumber;
            redirect->second.usRevisionNumber = reference.parsed_version.usRevisionNumber;
            if (std::find(raised_redirects.begin(), raised_redirects.end(), redirect) == raised_redirects.end())
            {
                raised_redirects.push_back(redirect);
            }
        }
        else
        {
            // Match AssemblyRef redirection: once an earlier reference has fixed the target, never lower a later
            // higher request. Leave it unchanged and let the runtime handle the incompatible versions.
            Logger::Error("UnsafeAccessorTypeAttributeUpdater: assembly [", redirect->first,
                          "] version=", AssemblyVersionStr(reference.parsed_version),
                          " is higher than an earlier applied redirection to version=", redirect->second.VersionStr());
        }
    }
    for (const auto redirect : raised_redirects)
    {
        // As with AssemblyRef redirection, an unchanged higher reference commits the promoted target even when this
        // function has no lower qualifier to rewrite.
        redirect->second.ulRedirectionCount++;
    }

    struct Edit
    {
        size_t      begin;
        size_t      end;
        std::string value;
        WSTRING     assembly_name;
    };
    std::vector<Edit> edits;
    for (const auto& resolved_reference : resolved_references)
    {
        const auto& reference = *resolved_reference.reference;
        const auto  redirect  = resolved_reference.redirect;
        if (reference.version_span && redirect->second.CompareToAssemblyVersion(reference.parsed_version) <= 0)
        {
            continue;
        }

        const auto target_version = VersionString(redirect->second);
        if (!reference.version_span)
        {
            // An explicit version prevents a lower TPA copy from binding before the managed resolver can participate.
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
    return true;
}

} // namespace trace

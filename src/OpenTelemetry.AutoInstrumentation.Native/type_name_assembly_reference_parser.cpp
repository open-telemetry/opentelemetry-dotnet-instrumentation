/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "type_name_assembly_reference_parser.h"

#include <optional>
#include <string>
#include <string_view>
#include <vector>

#include "ascii_string_utils.h"

namespace trace
{

// A SerString contains UTF-8 bytes. The reflection type-name delimiters inspected below are all ASCII, while every
// non-ASCII UTF-8 byte has its high bit set, so byte-wise syntax parsing is safe. Keeping whitespace and case handling
// ASCII-only also makes it independent of the host process locale.
Span Span::Trimmed(const std::string_view value, Span span)
{
    const auto is_ascii_space = [](const char character)
    {
        return character == ' ' || character == '\t' || character == '\n' || character == '\v' || character == '\f' ||
               character == '\r';
    };
    while (!span.Empty() && is_ascii_space(value[span.begin]))
    {
        span.begin++;
    }
    while (!span.Empty() && is_ascii_space(value[span.end - 1]))
    {
        span.end--;
    }
    return span;
}

namespace
{

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
    part            = Span::Trimmed(value, part);
    return true;
}

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
        argument        = Span::Trimmed(type_name, argument);
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

    ParsedAssemblyReference reference{Unescape(assembly_name.View(type_name)), assembly_name.end};
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
            EqualsIgnoreAsciiCase(Span::Trimmed(type_name, {qualifier.begin, *equals}).View(type_name),
                                  std::string_view("Version")))
        {
            const auto value       = Span::Trimmed(type_name, {*equals + 1, qualifier.end});
            reference.version_span = value;
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
    type = Span::Trimmed(type_name, type);
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

} // namespace

bool ParseTypeNameAssemblyReferences(const std::string_view type_name, std::vector<ParsedAssemblyReference>& references)
{
    references.clear();
    if (!ParseType(type_name, {0, type_name.size()}, 0, references))
    {
        references.clear();
        return false;
    }
    return true;
}

} // namespace trace

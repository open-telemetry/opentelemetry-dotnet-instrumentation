/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "unsafe_accessor_type_attribute_updater.h"

#include <algorithm>
#include <charconv>
#include <cstdint>
#include <limits>
#include <optional>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#include "logger.h"
#include "module_metadata.h"
#include "string_utils.h"

namespace trace
{

namespace
{

const WSTRING unsafe_accessor_type_attribute_name = WStr("System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute");
const WSTRING unsafe_accessor_attribute_name      = WStr("System.Runtime.CompilerServices.UnsafeAccessorAttribute");

// A custom-attribute SerString prefixes its UTF-8 bytes with an ECMA-335 compressed unsigned integer. Use the CLR's
// codec for that standard 1/2/4-byte representation, but guard its reader because it examines the first byte before
// validating the supplied length.
bool TryReadPackedLength(const BYTE* data, size_t size, ULONG& length, size_t& bytes_read)
{
    if (data == nullptr || size == 0)
    {
        return false;
    }

    uint32_t   decoded_length = 0;
    uint32_t   encoded_size   = 0;
    const auto result         = CorSigUncompressData(data, static_cast<DWORD>(size), &decoded_length, &encoded_size);
    if (FAILED(result))
    {
        return false;
    }

    length     = static_cast<ULONG>(decoded_length);
    bytes_read = encoded_size;
    return true;
}

bool WritePackedLength(std::vector<BYTE>& blob, size_t length)
{
    constexpr size_t max_packed_length = 0x1FFFFFFF;
    if (length > max_packed_length)
    {
        return false;
    }

    BYTE       encoded_length[4]{};
    const auto encoded_size = CorSigCompressData(static_cast<ULONG>(length), encoded_length);
    blob.insert(blob.end(), encoded_length, encoded_length + encoded_size);
    return true;
}

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

std::optional<size_t> FindUnescaped(const std::string_view value, const Span span, const char expected)
{
    auto position = span.begin;
    while (position < span.end)
    {
        if (value[position] == '\\' && position + 1 < span.end)
        {
            position += 2;
        }
        else if (value[position] == expected)
        {
            return position;
        }
        else
        {
            position++;
        }
    }
    return std::nullopt;
}

std::optional<size_t> FindTopLevelComma(const std::string_view type_name, const Span span)
{
    size_t bracket_depth = 0;
    auto   position      = span.begin;
    while (position < span.end)
    {
        const auto current = type_name[position];
        if (current == '\\' && position + 1 < span.end)
        {
            position += 2;
            continue;
        }
        if (current == '[')
        {
            bracket_depth++;
        }
        else if (current == ']' && bracket_depth > 0)
        {
            bracket_depth--;
        }
        else if (current == ',' && bracket_depth == 0)
        {
            return position;
        }
        position++;
    }

    return std::nullopt;
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
        if (type_name[position] == '\\' && position + 1 < end)
        {
            position += 2;
            continue;
        }
        if (type_name[position] == '[')
        {
            depth++;
        }
        else if (type_name[position] == ']' && --depth == 0)
        {
            return position;
        }
        position++;
    }

    return std::nullopt;
}

Span NextPart(const std::string_view value, Span& remaining, const char separator)
{
    const auto separator_position = FindUnescaped(value, remaining, separator);
    const Span part{remaining.begin, separator_position.value_or(remaining.end)};
    remaining.begin = separator_position ? *separator_position + 1 : remaining.end;
    return TrimmedSpan(value, part);
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
// exhaust the profiler's native stack; 64 is well above practical generic nesting
constexpr size_t max_generic_nesting_depth = 64;

bool FindAssemblyReferences(std::string_view                      type_name,
                            Span                                  type,
                            size_t                                generic_nesting_depth,
                            std::vector<ParsedAssemblyReference>& references);

bool FindGenericArguments(const std::string_view                type_name,
                          Span                                  arguments,
                          const size_t                          generic_nesting_depth,
                          std::vector<ParsedAssemblyReference>& references)
{
    while (!arguments.Empty())
    {
        const auto separator = FindTopLevelComma(type_name, arguments);
        Span       argument{arguments.begin, separator.value_or(arguments.end)};
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

        if (!FindAssemblyReferences(type_name, argument, generic_nesting_depth, references))
        {
            return false;
        }
    }
    return true;
}

bool FindAssemblyReferences(const std::string_view                type_name,
                            Span                                  type,
                            const size_t                          generic_nesting_depth,
                            std::vector<ParsedAssemblyReference>& references)
{
    type = TrimmedSpan(type_name, type);
    if (type.Empty())
    {
        return true;
    }

    // Example:
    // Outer`1[[Inner, Inner.Assembly, Version=1.0.0.0]], Outer.Assembly, Version=2.0.0.0
    // produces Inner.Assembly at 1.0.0.0 and Outer.Assembly at 2.0.0.0, in that order.
    const auto assembly_separator = FindTopLevelComma(type_name, type);
    const auto type_end           = assembly_separator.value_or(type.end);
    auto       position           = type.begin;
    while (position < type_end)
    {
        if (type_name[position] == '\\' && position + 1 < type_end)
        {
            position += 2;
            continue;
        }
        if (type_name[position] != '[')
        {
            position++;
            continue;
        }

        const auto closing_bracket = FindClosingBracket(type_name, position, type_end);
        if (!closing_bracket || generic_nesting_depth == max_generic_nesting_depth ||
            !FindGenericArguments(type_name, {position + 1, *closing_bracket}, generic_nesting_depth + 1, references))
        {
            return false;
        }
        position = *closing_bracket + 1;
    }

    if (!assembly_separator)
    {
        // An unqualified type resolves in the current assembly or CoreLib and has no assembly reference to redirect.
        return true;
    }

    Span       assembly{*assembly_separator + 1, type.end};
    const auto assembly_name = NextPart(type_name, assembly, ',');
    if (assembly_name.Empty())
    {
        return true;
    }

    ParsedAssemblyReference reference{assembly_name, assembly_name.end};
    while (!assembly.Empty())
    {
        const auto qualifier = NextPart(type_name, assembly, ',');
        const auto equals    = FindUnescaped(type_name, qualifier, '=');
        if (equals && EqualsIgnoreCase(TrimmedSpan(type_name, {qualifier.begin, *equals}).View(type_name), "Version"))
        {
            const auto value       = TrimmedSpan(type_name, {*equals + 1, qualifier.end});
            reference.version_span = value;
            if (!TryParseVersion(value.View(type_name), reference.parsed_version))
            {
                return false;
            }
            break;
        }
    }
    references.push_back(reference);
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

std::string VersionString(const AssemblyVersionRedirection& redirect)
{
    // Type names contain UTF-8, so format directly instead of creating and converting the WSTRING used by VersionStr.
    return std::to_string(redirect.usMajorVersion) + "." + std::to_string(redirect.usMinorVersion) + "." +
           std::to_string(redirect.usBuildNumber) + "." + std::to_string(redirect.usRevisionNumber);
}

bool IsConstructor(const ComPtr<IMetaDataImport2>& metadata_import, mdToken method)
{
    WCHAR   name[kNameMaxSize]{};
    ULONG   name_length = 0;
    HRESULT hr          = E_FAIL;
    if (TypeFromToken(method) == mdtMethodDef)
    {
        hr = metadata_import->GetMethodProps(method, nullptr, name, kNameMaxSize, &name_length, nullptr, nullptr,
                                             nullptr, nullptr, nullptr);
    }
    else if (TypeFromToken(method) == mdtMemberRef)
    {
        hr = metadata_import->GetMemberRefProps(method, nullptr, name, kNameMaxSize, &name_length, nullptr, nullptr);
    }
    return SUCCEEDED(hr) && name_length > 0 && WSTRING(name) == WStr(".ctor");
}

std::vector<mdToken> FindAttributeConstructors(const ComPtr<IMetaDataImport2>& metadata_import,
                                               const WSTRING&                  attribute_name)
{
    std::vector<mdToken> constructors;
    const auto           add_constructors = [&](const auto& methods)
    {
        for (const auto method : methods)
        {
            if (IsConstructor(metadata_import, method))
            {
                constructors.push_back(method);
            }
        }
    };
    mdTypeDef type_definition = mdTypeDefNil;
    if (metadata_import->FindTypeDefByName(attribute_name.c_str(), mdTokenNil, &type_definition) == S_OK)
    {
        add_constructors(EnumMethods(metadata_import, type_definition));
    }

    // A module can contain more than one matching TypeRef with different resolution scopes. Collect all of their
    // constructors so EnumCustomAttributes can filter at the metadata layer instead of inspecting every attribute.
    for (const auto type_reference : EnumTypeRefs(metadata_import))
    {
        WCHAR type_name[kNameMaxSize]{};
        ULONG type_name_length = 0;
        if (SUCCEEDED(metadata_import->GetTypeRefProps(type_reference, nullptr, type_name, kNameMaxSize,
                                                       &type_name_length)) &&
            type_name_length > 0 && WSTRING(type_name) == attribute_name)
        {
            add_constructors(EnumMemberRefs(metadata_import, type_reference));
        }
    }
    return constructors;
}

bool HasCustomAttribute(const ComPtr<IMetaDataImport2>& metadata_import, mdToken owner, const WCHAR* attribute_name)
{
    // GetCustomAttributeByName requires blob outputs even though existence is all the caller needs.
    const void* unused_blob      = nullptr;
    ULONG       unused_blob_size = 0;
    return metadata_import->GetCustomAttributeByName(owner, attribute_name, &unused_blob, &unused_blob_size) == S_OK;
}

} // namespace

bool HasUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import)
{
    mdTypeDef attribute_type = mdTypeDefNil;
    return metadata_import->FindTypeDefByName(unsafe_accessor_type_attribute_name.c_str(), mdTokenNil,
                                              &attribute_type) == S_OK;
}

bool TryRewriteUnsafeAccessorTypeName(const std::string_view                                   type_name,
                                      std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                      std::string&                                             rewritten_type_name,
                                      std::vector<WSTRING>& redirected_assembly_names)
{
    rewritten_type_name.clear();
    redirected_assembly_names.clear();

    std::vector<ParsedAssemblyReference> references;
    if (!FindAssemblyReferences(type_name, {0, type_name.size()}, 0, references))
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

bool TryRewriteUnsafeAccessorTypeAttributeBlob(
    const BYTE*                                              blob,
    ULONG                                                    blob_size,
    std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
    std::vector<BYTE>&                                       rewritten_blob,
    std::vector<WSTRING>&                                    redirected_assembly_names)
{
    rewritten_blob.clear();
    redirected_assembly_names.clear();

    constexpr size_t custom_attribute_prolog_size = 2;
    constexpr size_t named_argument_count_size    = 2;
    // The expected blob is: 01 00 prolog, one SerString constructor argument,
    // then at least the two-byte named-argument count.
    // A null SerString (FF) is not a type name and cannot be redirected.
    if (blob == nullptr || blob_size < custom_attribute_prolog_size + 1 + named_argument_count_size ||
        blob[0] != 0x01 || blob[1] != 0x00 || blob[custom_attribute_prolog_size] == 0xFF)
    {
        return false;
    }

    ULONG      string_length  = 0;
    size_t     length_bytes   = 0;
    const auto remaining_size = static_cast<size_t>(blob_size) - custom_attribute_prolog_size;
    if (!TryReadPackedLength(blob + custom_attribute_prolog_size, remaining_size, string_length, length_bytes))
    {
        return false;
    }

    // Validate the declared string range with subtraction (which cannot overflow) and require the trailing two-byte
    // named-argument count before constructing a string or advancing any pointers.
    const auto string_begin = custom_attribute_prolog_size + length_bytes;
    if (string_begin > blob_size || string_length > static_cast<size_t>(blob_size) - string_begin ||
        static_cast<size_t>(blob_size) - string_begin - string_length < named_argument_count_size)
    {
        return false;
    }

    const std::string_view type_name(reinterpret_cast<const char*>(blob + string_begin), string_length);
    std::string            rewritten_type_name;
    std::vector<WSTRING>   rewritten_assembly_names;
    if (!TryRewriteUnsafeAccessorTypeName(type_name, assembly_redirects, rewritten_type_name, rewritten_assembly_names))
    {
        return false;
    }

    // Rebuild rather than overwrite in place because the new version can cross a 1/2/4-byte length boundary. Preserve
    // every byte after the string instead of assuming it is only an empty named-argument count.
    const auto trailing_size = static_cast<size_t>(blob_size) - string_begin - string_length;
    rewritten_blob.insert(rewritten_blob.end(), blob, blob + custom_attribute_prolog_size);
    if (!WritePackedLength(rewritten_blob, rewritten_type_name.size()))
    {
        rewritten_blob.clear();
        return false;
    }

    // SetCustomAttributeValue takes an ULONG byte count. Use subtraction so this also remains safe when size_t is
    // 32-bit and the rebuilt value approaches that limit.
    constexpr auto max_blob_size = static_cast<size_t>((std::numeric_limits<ULONG>::max)());
    if (rewritten_type_name.size() > max_blob_size - rewritten_blob.size() ||
        trailing_size > max_blob_size - rewritten_blob.size() - rewritten_type_name.size())
    {
        rewritten_blob.clear();
        return false;
    }

    rewritten_blob.reserve(rewritten_blob.size() + rewritten_type_name.size() + trailing_size);
    rewritten_blob.insert(rewritten_blob.end(), rewritten_type_name.begin(), rewritten_type_name.end());
    rewritten_blob.insert(rewritten_blob.end(), blob + string_begin + string_length, blob + blob_size);
    redirected_assembly_names = std::move(rewritten_assembly_names);
    return true;
}

void UpdateUnsafeAccessorTypeAttributes(const ModuleMetadata&                                    module_metadata,
                                        std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects)
{
    if (assembly_redirects.empty())
    {
        return;
    }

    const auto& metadata_import  = module_metadata.metadata_import;
    const auto& metadata_emit    = module_metadata.metadata_emit;
    size_t      applicable_count = 0;
    size_t      updated_count    = 0;

    // Do not prefilter by the module's AssemblyRefs: reflection-style type names may be their only reference to a
    // redirected assembly. Resolve the attribute constructors once, then enumerate only attributes created by them.
    std::vector<mdCustomAttribute> attributes;
    for (const auto constructor : FindAttributeConstructors(metadata_import, unsafe_accessor_type_attribute_name))
    {
        for (const auto attribute : EnumCustomAttributes(metadata_import, mdTokenNil, constructor))
        {
            attributes.push_back(attribute);
        }
    }

    // EnumCustomAttributes is a lazy HCORENUM range. Materialize its tokens before changing metadata so no live
    // enumerator observes SetCustomAttributeValue.
    for (const auto attribute : attributes)
    {
        mdToken     owner     = mdTokenNil;
        const void* blob_data = nullptr;
        ULONG       blob_size = 0;
        const auto  attribute_hr =
            metadata_import->GetCustomAttributeProps(attribute, &owner, nullptr, &blob_data, &blob_size);
        if (FAILED(attribute_hr))
        {
            Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to read custom attribute in ",
                         module_metadata.assemblyName, ", HRESULT=", HResultStr(attribute_hr));
            continue;
        }

        // UnsafeAccessorType is valid only on parameters and return values;
        // both are represented by ParamDef tokens (the return value has sequence zero).
        if (TypeFromToken(owner) != mdtParamDef)
        {
            continue;
        }

        mdMethodDef method   = mdMethodDefNil;
        const auto  owner_hr = metadata_import->GetParamProps(owner, &method, nullptr, nullptr, 0, nullptr, nullptr,
                                                              nullptr, nullptr, nullptr);
        if (FAILED(owner_hr))
        {
            // A ParamDef should always identify its declaring method; failure indicates invalid or unreadable
            // metadata, so skip this attribute without jeopardizing module loading.
            Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to get the declaring method in ",
                         module_metadata.assemblyName, ", HRESULT=", HResultStr(owner_hr));
            continue;
        }

        if (!HasCustomAttribute(metadata_import, method, unsafe_accessor_attribute_name.c_str()))
        {
            continue;
        }

        applicable_count++;

        const auto*          blob = static_cast<const BYTE*>(blob_data);
        std::vector<BYTE>    rewritten_blob;
        std::vector<WSTRING> redirected_assembly_names;
        if (!TryRewriteUnsafeAccessorTypeAttributeBlob(blob, blob_size, assembly_redirects, rewritten_blob,
                                                       redirected_assembly_names))
        {
            continue;
        }

        const auto update_hr = metadata_emit->SetCustomAttributeValue(attribute, rewritten_blob.data(),
                                                                      static_cast<ULONG>(rewritten_blob.size()));
        if (FAILED(update_hr))
        {
            Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to update an attribute in ",
                         module_metadata.assemblyName, ", HRESULT=", HResultStr(update_hr));
            continue;
        }

        for (const auto& redirected_assembly_name : redirected_assembly_names)
        {
            const auto redirect = assembly_redirects.find(redirected_assembly_name);
            if (redirect != assembly_redirects.end())
            {
                // Commit state only after metadata mutation succeeds; later higher requests must know a previous
                // reference has already been rewritten.
                redirect->second.ulRedirectionCount++;
            }
        }
        updated_count++;
    }

    if (applicable_count > 0)
    {
        // Keep attributes that do not target the current redirection map visible in debug logs.
        Logger::Debug("UnsafeAccessorTypeAttributeUpdater: found ", applicable_count,
                      " applicable attribute(s), updated ", updated_count, " in ", module_metadata.assemblyName);
    }
    if (updated_count > 0)
    {
        Logger::Info("UnsafeAccessorTypeAttributeUpdater: updated ", updated_count, " attribute(s) in ",
                     module_metadata.assemblyName);
    }
}

} // namespace trace

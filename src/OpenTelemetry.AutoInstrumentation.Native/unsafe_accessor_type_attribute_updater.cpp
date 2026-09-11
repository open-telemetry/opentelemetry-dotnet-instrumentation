/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "unsafe_accessor_type_attribute_updater.h"

#include <algorithm>
#include <cctype>
#include <cstdint>
#include <limits>

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
    length                    = static_cast<ULONG>(decoded_length);
    bytes_read                = encoded_size;
    return SUCCEEDED(result);
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

size_t SkipWhitespace(const std::string& value, size_t position)
{
    while (position < value.size() && std::isspace(static_cast<unsigned char>(value[position])) != 0)
    {
        position++;
    }

    return position;
}

size_t TrimmedEnd(const std::string& value, size_t begin, size_t end)
{
    while (end > begin && std::isspace(static_cast<unsigned char>(value[end - 1])) != 0)
    {
        end--;
    }

    return end;
}

bool EqualsIgnoreCase(const std::string& left, const std::string& right)
{
    return left.size() == right.size() && std::equal(left.begin(), left.end(), right.begin(),
                                                     [](char left_char, char right_char)
                                                     {
                                                         return std::tolower(static_cast<unsigned char>(left_char)) ==
                                                                std::tolower(static_cast<unsigned char>(right_char));
                                                     });
}

size_t FindAssemblySeparator(const std::string& type_name)
{
    // The first unescaped comma at bracket depth zero starts the outer assembly qualifier. Ignore commas inside generic
    // arguments and escaped type-name commas.
    size_t bracket_depth = 0;
    bool   escaped       = false;

    for (size_t i = 0; i < type_name.size(); i++)
    {
        const auto current = type_name[i];
        if (escaped)
        {
            escaped = false;
            continue;
        }

        if (current == '\\')
        {
            escaped = true;
        }
        else if (current == '[')
        {
            bracket_depth++;
        }
        else if (current == ']' && bracket_depth > 0)
        {
            bracket_depth--;
        }
        else if (current == ',' && bracket_depth == 0)
        {
            return i;
        }
    }

    return std::string::npos;
}

bool TryParseVersion(const std::string& value, ASSEMBLYMETADATA& version)
{
    // Assembly metadata represents a version as exactly four unsigned 16-bit components.
    constexpr auto max_component = static_cast<unsigned int>((std::numeric_limits<USHORT>::max)());
    USHORT*        components[]  = {&version.usMajorVersion, &version.usMinorVersion, &version.usBuildNumber,
                                    &version.usRevisionNumber};
    size_t         position      = 0;

    for (size_t i = 0; i < 4; i++)
    {
        const auto separator = value.find('.', position);
        const auto end       = separator == std::string::npos ? value.size() : separator;
        if (end == position || (i < 3 && separator == std::string::npos) || (i == 3 && separator != std::string::npos))
        {
            return false;
        }

        unsigned int component = 0;
        for (size_t j = position; j < end; j++)
        {
            const auto current = value[j];
            if (current < '0' || current > '9')
            {
                return false;
            }

            const auto digit = static_cast<unsigned int>(current - '0');
            if (component > (max_component - digit) / 10)
            {
                return false;
            }

            component = (component * 10) + digit;
        }

        *components[i] = static_cast<USHORT>(component);
        position       = end + 1;
    }

    return true;
}

std::string VersionString(const AssemblyVersionRedirection& redirect)
{
    return std::to_string(redirect.usMajorVersion) + "." + std::to_string(redirect.usMinorVersion) + "." +
           std::to_string(redirect.usBuildNumber) + "." + std::to_string(redirect.usRevisionNumber);
}

bool IsUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import, mdToken constructor)
{
    // A constructor is a MethodDef when the attribute is defined in this module and a MemberRef when it is imported.
    mdToken parent = mdTokenNil;
    HRESULT hr     = E_FAIL;

    if (TypeFromToken(constructor) == mdtMethodDef)
    {
        hr = metadata_import->GetMethodProps(constructor, &parent, nullptr, 0, nullptr, nullptr, nullptr, nullptr,
                                             nullptr, nullptr);
    }
    else if (TypeFromToken(constructor) == mdtMemberRef)
    {
        hr = metadata_import->GetMemberRefProps(constructor, &parent, nullptr, 0, nullptr, nullptr, nullptr);
    }

    if (FAILED(hr))
    {
        return false;
    }

    WCHAR attr_name[kNameMaxSize]{};
    ULONG attr_name_length = 0;
    if (TypeFromToken(parent) == mdtTypeDef)
    {
        hr = metadata_import->GetTypeDefProps(parent, attr_name, kNameMaxSize, &attr_name_length, nullptr, nullptr);
    }
    else if (TypeFromToken(parent) == mdtTypeRef)
    {
        hr = metadata_import->GetTypeRefProps(parent, nullptr, attr_name, kNameMaxSize, &attr_name_length);
    }
    else
    {
        return false;
    }

    return SUCCEEDED(hr) && attr_name_length > 0 && WSTRING(attr_name) == unsafe_accessor_type_attribute_name;
}

} // namespace

bool HasUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import)
{
    mdTypeDef attribute_type = mdTypeDefNil;
    return metadata_import->FindTypeDefByName(unsafe_accessor_type_attribute_name.c_str(), mdTokenNil,
                                              &attribute_type) == S_OK;
}

bool TryRewriteUnsafeAccessorTypeName(const std::string&                                       type_name,
                                      std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
                                      std::string&                                             rewritten_type_name,
                                      WSTRING&                                                 redirected_assembly_name)
{
    const auto assembly_separator = FindAssemblySeparator(type_name);
    if (assembly_separator == std::string::npos)
    {
        // A type without an assembly qualifier cannot participate in assembly redirection.
        return false;
    }

    const auto assembly_begin = SkipWhitespace(type_name, assembly_separator + 1);
    const auto assembly_end   = type_name.find(',', assembly_begin);
    const auto assembly_name_end =
        TrimmedEnd(type_name, assembly_begin, assembly_end == std::string::npos ? type_name.size() : assembly_end);
    if (assembly_name_end == assembly_begin)
    {
        return false;
    }

    const auto assembly_name = type_name.substr(assembly_begin, assembly_name_end - assembly_begin);
    auto       redirect      = assembly_redirects.find(ToWSTRING(assembly_name));
    if (redirect == assembly_redirects.end())
    {
        redirect = std::find_if(assembly_redirects.begin(), assembly_redirects.end(),
                                [&assembly_name](const auto& entry)
                                { return EqualsIgnoreCase(ToString(entry.first), assembly_name); });
    }
    if (redirect == assembly_redirects.end())
    {
        return false;
    }

    auto qualifier = assembly_end;
    while (qualifier != std::string::npos)
    {
        // Preserve qualifier spelling and order; only replace the value of an existing Version qualifier.
        const auto qualifier_begin = SkipWhitespace(type_name, qualifier + 1);
        const auto qualifier_end   = type_name.find(',', qualifier_begin);
        const auto equals          = type_name.find('=', qualifier_begin);
        if (equals != std::string::npos && (qualifier_end == std::string::npos || equals < qualifier_end))
        {
            const auto key_end = TrimmedEnd(type_name, qualifier_begin, equals);
            if (EqualsIgnoreCase(type_name.substr(qualifier_begin, key_end - qualifier_begin), "Version"))
            {
                const auto value_begin = SkipWhitespace(type_name, equals + 1);
                const auto value_end =
                    TrimmedEnd(type_name, value_begin,
                               qualifier_end == std::string::npos ? type_name.size() : qualifier_end);
                ASSEMBLYMETADATA existing_version{};
                if (!TryParseVersion(type_name.substr(value_begin, value_end - value_begin), existing_version))
                {
                    // Do not guess at malformed input; leave it for the runtime to reject or resolve.
                    return false;
                }

                // Keep the version-ordering policy aligned with CorProfiler::RedirectAssemblyReferences.
                const auto version_comparison = redirect->second.CompareToAssemblyVersion(existing_version);
                if (version_comparison <= 0)
                {
                    // Never lower a requested version. Before any redirect is committed, let this higher request
                    // raise the shared target; afterward, changing it could disagree with metadata already rewritten.
                    if (version_comparison < 0)
                    {
                        if (redirect->second.ulRedirectionCount == 0)
                        {
                            Logger::Info("UnsafeAccessorTypeAttributeUpdater: redirection update for [",
                                         redirect->first, "] to_version=", AssemblyVersionStr(existing_version),
                                         " previous_version_redirection=", redirect->second.VersionStr());
                            redirect->second.usMajorVersion   = existing_version.usMajorVersion;
                            redirect->second.usMinorVersion   = existing_version.usMinorVersion;
                            redirect->second.usBuildNumber    = existing_version.usBuildNumber;
                            redirect->second.usRevisionNumber = existing_version.usRevisionNumber;
                            // The unchanged higher reference is now the committed target for subsequent rewrites.
                            redirect->second.ulRedirectionCount++;
                        }
                        else
                        {
                            Logger::Error("UnsafeAccessorTypeAttributeUpdater: assembly [", redirect->first,
                                          "] version=", AssemblyVersionStr(existing_version),
                                          " is higher than an earlier applied redirection to version=",
                                          redirect->second.VersionStr());
                        }
                    }

                    return false;
                }

                const auto target_version = VersionString(redirect->second);
                rewritten_type_name = type_name.substr(0, value_begin) + target_version + type_name.substr(value_end);
                redirected_assembly_name = redirect->first;
                return true;
            }
        }

        qualifier = qualifier_end;
    }

    // A versionless request can bind to the runtime's lower trusted platform assembly (TPA) copy without invoking the
    // managed resolver. Adding an explicit higher version lets the resolver supply the instrumentation copy.
    const auto target_version = VersionString(redirect->second);
    rewritten_type_name =
        type_name.substr(0, assembly_name_end) + ", Version=" + target_version + type_name.substr(assembly_name_end);
    redirected_assembly_name = redirect->first;
    return true;
}

bool TryRewriteUnsafeAccessorTypeAttributeBlob(
    const BYTE*                                              blob,
    ULONG                                                    blob_size,
    std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects,
    std::vector<BYTE>&                                       rewritten_blob,
    WSTRING&                                                 redirected_assembly_name)
{
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
    if (string_length > static_cast<size_t>(blob_size) - string_begin ||
        static_cast<size_t>(blob_size) - string_begin - string_length < named_argument_count_size)
    {
        return false;
    }

    const std::string type_name(reinterpret_cast<const char*>(blob + string_begin), string_length);
    std::string       rewritten_type_name;
    if (!TryRewriteUnsafeAccessorTypeName(type_name, assembly_redirects, rewritten_type_name, redirected_assembly_name))
    {
        return false;
    }

    // Rebuild rather than overwrite in place because the new version can cross a 1/2/4-byte length boundary. Preserve
    // every byte after the string instead of assuming it is only an empty named-argument count.
    const auto trailing_size = static_cast<size_t>(blob_size) - string_begin - string_length;
    rewritten_blob.clear();
    rewritten_blob.insert(rewritten_blob.end(), blob, blob + custom_attribute_prolog_size);
    if (!WritePackedLength(rewritten_blob, rewritten_type_name.size()))
    {
        rewritten_blob.clear();
        return false;
    }

    constexpr auto max_blob_size = static_cast<size_t>((std::numeric_limits<ULONG>::max)());
    if (trailing_size > max_blob_size - rewritten_blob.size())
    {
        rewritten_blob.clear();
        return false;
    }

    const auto fixed_size = rewritten_blob.size() + trailing_size;
    if (rewritten_type_name.size() > max_blob_size - fixed_size)
    {
        rewritten_blob.clear();
        return false;
    }

    rewritten_blob.reserve(fixed_size + rewritten_type_name.size());
    rewritten_blob.insert(rewritten_blob.end(), rewritten_type_name.begin(), rewritten_type_name.end());
    rewritten_blob.insert(rewritten_blob.end(), blob + string_begin + string_length, blob + blob_size);
    return true;
}

void UpdateUnsafeAccessorTypeAttributes(const ModuleMetadata&                                    module_metadata,
                                        std::unordered_map<WSTRING, AssemblyVersionRedirection>& assembly_redirects)
{
    const auto&       metadata_import = module_metadata.metadata_import;
    const auto&       metadata_emit   = module_metadata.metadata_emit;
    HCORENUM          attribute_enum  = nullptr;
    mdCustomAttribute attributes[32];
    ULONG             applicable_count = 0;
    ULONG             updated_count    = 0;
    HRESULT           hr               = S_OK;

    while (hr == S_OK)
    {
        ULONG attribute_count = 0;
        hr = metadata_import->EnumCustomAttributes(&attribute_enum, mdTokenNil, mdTokenNil, attributes, 32,
                                                   &attribute_count);
        if (FAILED(hr))
        {
            Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to enumerate custom attributes in ",
                         module_metadata.assemblyName, ", HRESULT=", HResultStr(hr));
            break;
        }

        for (ULONG i = 0; i < attribute_count; i++)
        {
            mdToken     owner       = mdTokenNil;
            mdToken     constructor = mdTokenNil;
            const void* blob_data   = nullptr;
            ULONG       blob_size   = 0;
            const auto  attribute_hr =
                metadata_import->GetCustomAttributeProps(attributes[i], &owner, &constructor, &blob_data, &blob_size);
            if (FAILED(attribute_hr))
            {
                Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to read custom attribute in ",
                             module_metadata.assemblyName, ", HRESULT=", HResultStr(attribute_hr));
                continue;
            }

            const auto* blob = static_cast<const BYTE*>(blob_data);

            // UnsafeAccessorType is valid only on parameters and return values;
            // both are represented by ParamDef tokens (the return value has sequence zero).
            if (TypeFromToken(owner) != mdtParamDef)
            {
                continue;
            }

            if (!IsUnsafeAccessorTypeAttribute(metadata_import, constructor))
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

            const void* unsafe_accessor_blob      = nullptr;
            ULONG       unsafe_accessor_blob_size = 0;
            if (metadata_import->GetCustomAttributeByName(method, unsafe_accessor_attribute_name.c_str(),
                                                          &unsafe_accessor_blob, &unsafe_accessor_blob_size) != S_OK)
            {
                continue;
            }

            applicable_count++;

            std::vector<BYTE> rewritten_blob;
            WSTRING           redirected_assembly_name;
            if (!TryRewriteUnsafeAccessorTypeAttributeBlob(blob, blob_size, assembly_redirects, rewritten_blob,
                                                           redirected_assembly_name))
            {
                continue;
            }

            const auto update_hr = metadata_emit->SetCustomAttributeValue(attributes[i], rewritten_blob.data(),
                                                                          static_cast<ULONG>(rewritten_blob.size()));
            if (FAILED(update_hr))
            {
                Logger::Warn("UnsafeAccessorTypeAttributeUpdater: failed to update an attribute in ",
                             module_metadata.assemblyName, ", HRESULT=", HResultStr(update_hr));
                continue;
            }

            const auto redirect = assembly_redirects.find(redirected_assembly_name);
            if (redirect != assembly_redirects.end())
            {
                // Commit state only after metadata mutation succeeds; later higher requests must know a previous
                // reference has already been rewritten.
                redirect->second.ulRedirectionCount++;
            }
            updated_count++;
        }
    }

    metadata_import->CloseEnum(attribute_enum);
    if (applicable_count > 0)
    {
        // This debug summary also makes the separate CoreLib scan visible when none of its attributes target an
        // assembly in the current redirection map.
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

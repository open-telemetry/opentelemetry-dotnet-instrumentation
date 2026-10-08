/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "unsafe_accessor_type_attribute_updater.h"

#include <cstdint>
#include <limits>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#include "logger.h"
#include "module_metadata.h"
#include "type_name_assembly_redirection_rewriter.h"

namespace trace
{

namespace
{

const WSTRING unsafe_accessor_type_attribute_name = WStr("System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute");
const WSTRING unsafe_accessor_attribute_name      = WStr("System.Runtime.CompilerServices.UnsafeAccessorAttribute");

bool WritePackedLength(std::vector<BYTE>& blob, const size_t length)
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

} // namespace

bool HasUnsafeAccessorTypeAttribute(const ComPtr<IMetaDataImport2>& metadata_import)
{
    mdTypeDef attribute_type = mdTypeDefNil;
    return metadata_import->FindTypeDefByName(unsafe_accessor_type_attribute_name.c_str(), mdTokenNil,
                                              &attribute_type) == S_OK;
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

    // CorSigUncompressData must only see the SerString, excluding the trailing named-argument count.
    const auto serialized_string_size =
        static_cast<size_t>(blob_size) - custom_attribute_prolog_size - named_argument_count_size;
    // The checked CorSigUncompressData overload uses fixed-width uint32_t outputs.
    uint32_t string_length       = 0;
    uint32_t encoded_length_size = 0;
    if (FAILED(CorSigUncompressData(blob + custom_attribute_prolog_size, static_cast<DWORD>(serialized_string_size),
                                    &string_length, &encoded_length_size)) ||
        encoded_length_size > serialized_string_size || string_length > serialized_string_size - encoded_length_size)
    {
        return false;
    }

    const auto             string_begin = custom_attribute_prolog_size + static_cast<size_t>(encoded_length_size);
    const std::string_view type_name(reinterpret_cast<const char*>(blob + string_begin), string_length);
    std::string            rewritten_type_name;
    std::vector<WSTRING>   rewritten_assembly_names;
    if (!TryRewriteTypeNameAssemblyRedirections(type_name, assembly_redirects, rewritten_type_name,
                                                rewritten_assembly_names))
    {
        return false;
    }

    // Rebuild rather than overwrite in place because the new version can cross a 1/2/4-byte length boundary. Preserve
    // every byte after the string instead of assuming it is only an empty named-argument count.
    const auto trailing_size =
        serialized_string_size - static_cast<size_t>(encoded_length_size) - string_length + named_argument_count_size;
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

        // [UnsafeAccessorType] attribute can occur by itself in metadata, but only its pairing with [UnsafeAccessor]
        // makes the parameter or return value part of a JIT-level unsafe-accessor declaration.
        // Leave unrelated metadata untouched even though it could be rewritten safely.
        if (metadata_import->GetCustomAttributeByName(method, unsafe_accessor_attribute_name.c_str(), nullptr,
                                                      nullptr) != S_OK)
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
        if (updated_count > 0)
        {
            Logger::Info("UnsafeAccessorTypeAttributeUpdater: found ", applicable_count,
                         " applicable attribute(s), updated ", updated_count, " in ", module_metadata.assemblyName);
        }
        else
        {
            Logger::Debug("UnsafeAccessorTypeAttributeUpdater: found ", applicable_count,
                          " applicable attribute(s), updated ", updated_count, " in ", module_metadata.assemblyName);
        }
    }
}

} // namespace trace

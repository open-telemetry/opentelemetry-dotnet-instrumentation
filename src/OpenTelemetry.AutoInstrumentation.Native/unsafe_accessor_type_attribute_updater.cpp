/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "unsafe_accessor_type_attribute_updater.h"

#include <unordered_map>
#include <vector>

#include "logger.h"
#include "module_metadata.h"
#include "unsafe_accessor_type_attribute_blob_rewriter.h"

namespace trace
{

namespace
{

const WSTRING unsafe_accessor_type_attribute_name = WStr("System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute");
const WSTRING unsafe_accessor_attribute_name      = WStr("System.Runtime.CompilerServices.UnsafeAccessorAttribute");

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
                // Record only metadata that was actually changed, so the shared map cannot later select a different
                // target than the version already written into this attribute.
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

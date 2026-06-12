/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_
#define OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

#include <vector>
#include <string>
#include <windows.h> // Required for WideCharToMultiByte

#include "cor_profiler.h"
#include "logger.h"
#include "util.h"
#include "clr_helpers.h"

namespace trace
{

// Helper to encode PackedLen (CorSigCompress)
void WritePackedLen(std::vector<BYTE>& blob, ULONG len)
{
    if (len <= 0x7F)
    {
        blob.push_back(static_cast<BYTE>(len));
    }
    else if (len <= 0x3FFF)
    {
        blob.push_back(static_cast<BYTE>((len >> 8) | 0x80));
        blob.push_back(static_cast<BYTE>(len & 0xFF));
    }
    else
    {
        blob.push_back(static_cast<BYTE>((len >> 24) | 0xC0));
        blob.push_back(static_cast<BYTE>((len >> 16) & 0xFF));
        blob.push_back(static_cast<BYTE>((len >> 8) & 0xFF));
        blob.push_back(static_cast<BYTE>(len & 0xFF));
    }
}

// Helper to read PackedLen and return the offset shift
ULONG ReadPackedLen(const BYTE* ptr, ULONG* pLen)
{
    if ((ptr[0] & 0x80) == 0x00)
    {
        *pLen = ptr[0];
        return 1;
    }
    else if ((ptr[0] & 0xC0) == 0x80)
    {
        *pLen = ((ptr[0] & 0x3F) << 8) | ptr[1];
        return 2;
    }
    else
    {
        *pLen = ((ptr[0] & 0x1F) << 24) | (ptr[1] << 16) | (ptr[2] << 8) | ptr[3];
        return 4;
    }
}

// Helper: Convert WCHAR to std::string (UTF-8) for logging
std::string ToUTF8(const WCHAR* wstr)
{
    if (!wstr || wstr[0] == L'\0')
        return "";
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, NULL, 0, NULL, NULL);
    if (size_needed <= 0)
        return "";
    std::string strTo(size_needed - 1, 0);
    WideCharToMultiByte(CP_UTF8, 0, wstr, -1, &strTo[0], size_needed, NULL, NULL);
    return strTo;
}

inline void UpdateUnsafeAccessorTypeAttributes(const ModuleMetadata& module_metadata)
{
    const auto&       metadata_import = module_metadata.metadata_import;
    const auto&       metadata_emit   = module_metadata.metadata_emit;
    const std::string log_prefix      = "UnsafeAccessorTypeAttributeUpdater: ";
    // TODO for POC just use a fixed name, but the final implementation will add the redirected version to the exiting name
    const std::string new_type_name   = "UnknownType, UnknowAssembly";

    // 1. Build the replacement blob
    std::vector<BYTE> new_blob;
    new_blob.push_back(0x01);
    new_blob.push_back(0x00);
    WritePackedLen(new_blob, static_cast<ULONG>(new_type_name.size()));
    for (char c : new_type_name)
        new_blob.push_back(static_cast<BYTE>(c));
    new_blob.push_back(0x00);
    new_blob.push_back(0x00);

    HCORENUM  type_enum = NULL;
    mdTypeDef type_defs[64];
    ULONG     type_count = 0;

    while (metadata_import->EnumTypeDefs(&type_enum, type_defs, 64, &type_count) == S_OK && type_count > 0)
    {
        for (ULONG ti = 0; ti < type_count; ti++)
        {
            HCORENUM    method_enum = NULL;
            mdMethodDef methods[64];
            ULONG       method_count = 0;

            while (metadata_import->EnumMethods(&method_enum, type_defs[ti], methods, 64, &method_count) == S_OK &&
                   method_count > 0)
            {
                for (ULONG mi = 0; mi < method_count; mi++)
                {
                    const void* dummy_data;
                    ULONG       dummy_size;
                    if (metadata_import
                            ->GetCustomAttributeByName(methods[mi],
                                                       WStr("System.Runtime.CompilerServices.UnsafeAccessorAttribute"),
                                                       &dummy_data, &dummy_size) != S_OK)
                        continue;

                    // 2. Identify Method and Class
                    WCHAR     m_name_w[256], c_name_w[256];
                    mdTypeDef parent_type;
                    metadata_import->GetMethodProps(methods[mi], &parent_type, m_name_w, 256, nullptr, nullptr, nullptr,
                                                    nullptr, nullptr, nullptr);
                    metadata_import->GetTypeDefProps(parent_type, c_name_w, 256, nullptr, nullptr, nullptr);

                    Logger::Info(log_prefix, "Found method with [UnsafeAccessor]: ", ToUTF8(c_name_w), ".",
                                 ToUTF8(m_name_w));

                    // Track if we actually update anything on this method
                    bool any_updated = false;

                    HCORENUM   param_enum = NULL;
                    mdParamDef params[32];
                    ULONG      param_count = 0;

                    while (metadata_import->EnumParams(&param_enum, methods[mi], params, 32, &param_count) == S_OK &&
                           param_count > 0)
                    {
                        for (ULONG pi = 0; pi < param_count; pi++)
                        {
                            ULONG sequence;
                            metadata_import->GetParamProps(params[pi], nullptr, &sequence, nullptr, 0, nullptr, nullptr,
                                                           nullptr, nullptr, nullptr);

                            HCORENUM          ca_enum = NULL;
                            mdCustomAttribute ca_tokens[16];
                            ULONG             ca_count = 0;

                            while (metadata_import->EnumCustomAttributes(&ca_enum, params[pi], mdTokenNil, ca_tokens,
                                                                         16, &ca_count) == S_OK &&
                                   ca_count > 0)
                            {
                                for (ULONG ci = 0; ci < ca_count; ci++)
                                {
                                    mdToken     ca_ctor;
                                    const BYTE* ca_blob;
                                    ULONG       ca_blob_size;

                                    if (FAILED(metadata_import->GetCustomAttributeProps(ca_tokens[ci], nullptr,
                                                                                        &ca_ctor,
                                                                                        (const void**)&ca_blob,
                                                                                        &ca_blob_size)))
                                        continue;

                                    // 3. Resolve Attribute Name
                                    WCHAR   attr_name_w[512] = {0};
                                    mdToken parent           = mdTokenNil;
                                    if (TypeFromToken(ca_ctor) == mdtMethodDef)
                                        metadata_import->GetMethodProps(ca_ctor, &parent, nullptr, 0, nullptr, nullptr,
                                                                        nullptr, nullptr, nullptr, nullptr);
                                    else if (TypeFromToken(ca_ctor) == mdtMemberRef)
                                        metadata_import->GetMemberRefProps(ca_ctor, &parent, nullptr, 0, nullptr,
                                                                           nullptr, nullptr);

                                    if (TypeFromToken(parent) == mdtTypeDef)
                                        metadata_import->GetTypeDefProps(parent, attr_name_w, 512, nullptr, nullptr,
                                                                         nullptr);
                                    else if (TypeFromToken(parent) == mdtTypeRef)
                                        metadata_import->GetTypeRefProps(parent, nullptr, attr_name_w, 512, nullptr);

                                    if (wcscmp(attr_name_w,
                                               L"System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute") != 0)
                                        continue;

                                    // 4. Extract Old Name
                                    std::string old_name_str = "<empty>";
                                    if (ca_blob_size >= 3 && ca_blob[0] == 0x01 && ca_blob[1] == 0x00 &&
                                        ca_blob[2] != 0xFF)
                                    {
                                        ULONG str_len  = 0;
                                        ULONG len_size = ReadPackedLen(&ca_blob[2], &str_len);
                                        if (2 + len_size + str_len <= ca_blob_size && str_len > 0)
                                        {
                                            old_name_str = std::string((const char*)&ca_blob[2 + len_size], str_len);
                                        }
                                    }

                                    // 5. Apply Substitution
                                    HRESULT hr = metadata_emit->SetCustomAttributeValue(ca_tokens[ci], new_blob.data(),
                                                                                        (ULONG)new_blob.size());

                                    if (SUCCEEDED(hr))
                                    {
                                        any_updated = true;
                                        std::string target =
                                            (sequence == 0 ? "ReturnVal" : "Param#" + std::to_string(sequence));
                                        Logger::Info(log_prefix, "  -> Updated ", target,
                                                     " [UnsafeAccessorType]: ", old_name_str, " -> ", new_type_name);
                                    }
                                    else
                                    {
                                        Logger::Warn(log_prefix, "  !! Failed to update [UnsafeAccessorType] on ",
                                                     ToUTF8(c_name_w), ".", ToUTF8(m_name_w), " HRESULT: 0x", std::hex,
                                                     hr);
                                    }
                                }
                            }
                            metadata_import->CloseEnum(ca_enum);
                        }
                    }
                    metadata_import->CloseEnum(param_enum);

                    // 6. Log if no UnsafeAccessorType attributes were found at all
                    if (!any_updated)
                    {
                        Logger::Info(log_prefix, "  -> No [UnsafeAccessorType] attributes found on this method.");
                    }
                }
            }
            metadata_import->CloseEnum(method_enum);
        }
    }
    metadata_import->CloseEnum(type_enum);
}
} // namespace trace

#endif  // OTEL_CLR_PROFILER_UNSAFE_ACCESSOR_TYPE_ATTRIBUTE_UPDATER_H_

/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "unsafe_accessor_type_attribute_blob_rewriter.h"

#include <cstdint>
#include <limits>
#include <string>
#include <string_view>
#include <unordered_map>
#include <utility>
#include <vector>

#include "type_name_assembly_redirection_rewriter.h"

namespace trace
{

namespace
{

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

} // namespace

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

} // namespace trace

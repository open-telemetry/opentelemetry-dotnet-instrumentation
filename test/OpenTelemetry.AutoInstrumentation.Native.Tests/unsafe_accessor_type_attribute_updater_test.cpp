#include "pch.h"

#include <stdexcept>

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/unsafe_accessor_type_attribute_updater.h"

using namespace trace;

namespace
{

constexpr size_t one_byte_length_max = 0x7F;
constexpr size_t two_byte_length_max = 0x3FFF;

// This creates the complete UnsafeAccessorTypeAttribute value blob:
// 1) custom-attribute prolog,
// 2) one string constructor argument,
// 3) zero named-argument count.
// This test encoder remains independent from production.
std::vector<BYTE> CreateUnsafeAccessorTypeAttributeBlob(const std::string& type_name)
{
    std::vector<BYTE> blob;
    // 1) custom-attribute prolog as fixed 0x0001 in little-endian order.
    blob.push_back(0x01);
    blob.push_back(0x00);

    // 2) one string constructor argument - a type name as an ECMA-335 SerString: a compressed UTF-8 byte length
    // followed by the UTF-8 bytes. Encoding the length here keeps the expected test blobs independent from the
    // production CLR codec.
    const auto type_name_length = type_name.size();
    if (type_name_length <= one_byte_length_max)
    {
        blob.push_back(static_cast<BYTE>(type_name_length));
    }
    else if (type_name_length <= two_byte_length_max)
    {
        blob.push_back(static_cast<BYTE>((type_name_length >> 8) | 0x80));
        blob.push_back(static_cast<BYTE>(type_name_length));
    }
    else if (type_name_length <= 0x1FFFFFFF)
    {
        blob.push_back(static_cast<BYTE>((type_name_length >> 24) | 0xC0));
        blob.push_back(static_cast<BYTE>(type_name_length >> 16));
        blob.push_back(static_cast<BYTE>(type_name_length >> 8));
        blob.push_back(static_cast<BYTE>(type_name_length));
    }
    else
    {
        throw std::invalid_argument("Serialized string is too long for an ECMA-335 compressed length.");
    }

    blob.insert(blob.end(), type_name.begin(), type_name.end());

    // 3) zero named-argument count as a 16-bit little-endian value.
    blob.push_back(0x00);
    blob.push_back(0x00);
    return blob;
}

} // namespace

TEST(UnsafeAccessorTypeNameRewriterTest, AddsMissingVersion)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Redirected.Assembly", redirects, rewritten_type_name,
                                                 rewritten_assembly_name));
    EXPECT_EQ(rewritten_type_name, "Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
    EXPECT_EQ(rewritten_assembly_name, WStr("Test.Redirected.Assembly"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, MatchesAssemblyNameCaseInsensitively)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeName("Test.Type, test.redirected.assembly", redirects, rewritten_type_name,
                                                 rewritten_assembly_name));
    EXPECT_EQ(rewritten_type_name, "Test.Type, test.redirected.assembly, Version=11.0.0.0");
    EXPECT_EQ(rewritten_assembly_name, WStr("Test.Redirected.Assembly"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, ReplacesLowerVersionAndPreservesQualifiers)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeName(
        "Test.Type, Test.Redirected.Assembly, Culture=neutral, Version=1.12345.12345.12345, "
        "PublicKeyToken=0123456789abcdef",
        redirects, rewritten_type_name, rewritten_assembly_name));
    EXPECT_EQ(rewritten_type_name, "Test.Type, Test.Redirected.Assembly, Culture=neutral, Version=11.0.0.0, "
                                   "PublicKeyToken=0123456789abcdef");
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesEqualVersion)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0", redirects,
                                                  rewritten_type_name, rewritten_assembly_name));
    auto& redirect = redirects.at(WStr("Test.Redirected.Assembly"));
    EXPECT_EQ(redirect.VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, FindsOuterAssemblyAfterGenericArguments)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeName(
        "Test.GenericType`1[[Test.OtherType, Test.Other.Assembly]], Test.Redirected.Assembly", redirects,
        rewritten_type_name, rewritten_assembly_name));
    EXPECT_EQ(rewritten_type_name,
              "Test.GenericType`1[[Test.OtherType, Test.Other.Assembly]], Test.Redirected.Assembly, "
              "Version=11.0.0.0");
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteAssemblyNestedInsideGenericArguments)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName(
        "Test.GenericType`1[[Test.OtherType, Test.Redirected.Assembly, Version=1.0.0.0]], Test.Other.Assembly",
        redirects, rewritten_type_name, rewritten_assembly_name));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteUnmappedAssembly)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Other.Assembly", redirects, rewritten_type_name,
                                                  rewritten_assembly_name));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteUnqualifiedType)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName("Test.Type", redirects, rewritten_type_name, rewritten_assembly_name));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteMalformedVersion)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Redirected.Assembly, Version=11.0.invalid.0",
                                                  redirects, rewritten_type_name, rewritten_assembly_name));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PromotesMapForHigherVersionBeforeFirstRedirection)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Redirected.Assembly, Version=12.0.0.0", redirects,
                                                  rewritten_type_name, rewritten_assembly_name));
    auto& redirect = redirects.at(WStr("Test.Redirected.Assembly"));
    EXPECT_EQ(redirect.VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirect.ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotPromoteMapAfterRedirectionWasApplied)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    redirects.at(WStr("Test.Redirected.Assembly")).ulRedirectionCount = 1;
    std::string rewritten_type_name;
    WSTRING     rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName("Test.Type, Test.Redirected.Assembly, Version=12.0.0.0", redirects,
                                                  rewritten_type_name, rewritten_assembly_name));
    auto& redirect = redirects.at(WStr("Test.Redirected.Assembly"));
    EXPECT_EQ(redirect.VersionStr(), WStr("11.0.0.0"));
    EXPECT_EQ(redirect.ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWithoutLeavingBytesFromLongerValue)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    const auto original_suffix = std::string(", Test.Redirected.Assembly, Version=1.12345.12345.12345");
    const auto expected_suffix = std::string(", Test.Redirected.Assembly, Version=11.0.0.0");
    const auto type_prefix     = std::string(one_byte_length_max - expected_suffix.size(), 'N');
    const auto original_name   = type_prefix + original_suffix;
    const auto expected_name   = type_prefix + expected_suffix;
    ASSERT_GT(original_name.size(), one_byte_length_max);
    ASSERT_EQ(expected_name.size(), one_byte_length_max);
    const auto        original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto        expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE> rewritten_blob;
    WSTRING           rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_name));
    EXPECT_EQ(rewritten_blob, expected_blob);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWhenValueNeedsTwoByteLength)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    const auto assembly_suffix = std::string(", Test.Redirected.Assembly");
    const auto type_prefix     = std::string(one_byte_length_max - assembly_suffix.size(), 'N');
    const auto original_name   = type_prefix + assembly_suffix;
    const auto expected_name   = original_name + ", Version=11.0.0.0";
    ASSERT_EQ(original_name.size(), one_byte_length_max);
    ASSERT_GT(expected_name.size(), one_byte_length_max);
    const auto        original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto        expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE> rewritten_blob;
    WSTRING           rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_name));
    EXPECT_EQ(rewritten_blob, expected_blob);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWhenValueNeedsFourByteLength)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    const auto assembly_suffix = std::string(", Test.Redirected.Assembly");
    const auto type_prefix     = std::string(two_byte_length_max - assembly_suffix.size(), 'N');
    const auto original_name   = type_prefix + assembly_suffix;
    const auto expected_name   = original_name + ", Version=11.0.0.0";
    ASSERT_EQ(original_name.size(), two_byte_length_max);
    ASSERT_GT(expected_name.size(), two_byte_length_max);
    const auto        original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto        expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE> rewritten_blob;
    WSTRING           rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_name));
    EXPECT_EQ(rewritten_blob, expected_blob);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, PreservesTrailingBlobBytes)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    const auto original_name = std::string("Test.Type, Test.Redirected.Assembly, Version=1.0.0.0");
    const auto expected_name = std::string("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
    auto       original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    auto       expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    // Opaque marker bytes make this distinguish preservation from rebuilding a hardcoded 00 00 suffix.
    const std::vector<BYTE> trailing_marker = {0xAA, 0xBB, 0xCC};
    original_blob.insert(original_blob.end(), trailing_marker.begin(), trailing_marker.end());
    expected_blob.insert(expected_blob.end(), trailing_marker.begin(), trailing_marker.end());
    std::vector<BYTE> rewritten_blob;
    WSTRING           rewritten_assembly_name;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_name));
    EXPECT_EQ(rewritten_blob, expected_blob);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RejectsTruncatedPackedLength)
{
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {
        {WStr("Test.Redirected.Assembly"), {11, 0, 0, 0}}};
    const std::vector<BYTE> truncated_blob = {0x01, 0x00, 0xC0, 0x00, 0x00};
    std::vector<BYTE>       rewritten_blob;
    WSTRING                 rewritten_assembly_name;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeAttributeBlob(truncated_blob.data(),
                                                           static_cast<ULONG>(truncated_blob.size()), redirects,
                                                           rewritten_blob, rewritten_assembly_name));
}

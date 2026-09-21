#include "pch.h"

#include <stdexcept>

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/string_utils.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/unsafe_accessor_type_attribute_updater.h"

using namespace trace;

namespace
{

constexpr size_t one_byte_length_max       = 0x7F;
constexpr size_t two_byte_length_max       = 0x3FFF;
constexpr size_t max_generic_nesting_depth = 64;

std::string WrapInGenericTypes(std::string type_name, const size_t depth)
{
    for (size_t index = 0; index < depth; index++)
    {
        type_name = "Test.Generic`1[[" + type_name + "]]";
    }
    return type_name;
}

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

// Name-rewriter tests cover supported reflection type-name forms. For a given input and redirection map, they validate:
// - whether a rewrite happened,
// - the rewritten type name,
// - the assemblies reported as rewritten,
// - the resulting target versions in the redirection map, and
// - the redirect count where the scenario changes or preconfigures it.
TEST(UnsafeAccessorTypeNameRewriterTest, AddsMissingVersion)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto           input_type_name    = std::string("Test.Type, Test.Redirected.Assembly");
    const auto           expected_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, MatchesAssemblyNameCaseInsensitively)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto           input_type_name    = std::string("Test.Type, test.redirected.assembly");
    const auto           expected_type_name = std::string("Test.Type, test.redirected.assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, ReplacesLowerVersionAndPreservesQualifiers)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Type, Test.Redirected.Assembly, Culture=neutral, Version=1.12345.12345.12345, "
                    "PublicKeyToken=0123456789abcdef");
    const auto expected_type_name =
        std::string("Test.Type, Test.Redirected.Assembly, Culture=neutral, Version=11.0.0.0, "
                    "PublicKeyToken=0123456789abcdef");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesIrregularSpacing)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto  input_type_name    = std::string("Test.Type  ,   Test.Redirected.Assembly  ,   Version  =  1.0.0.0");
    const auto  expected_type_name = std::string("Test.Type  ,   Test.Redirected.Assembly  ,   Version  =  11.0.0.0");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesEqualVersion)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto           input_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, FindsOuterAssemblyAfterGenericArguments)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Generic`1[[Test.Other, Test.Other.Assembly]], Test.Redirected.Assembly");
    const auto expected_type_name =
        std::string("Test.Generic`1[[Test.Other, Test.Other.Assembly]], Test.Redirected.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, AddsMissingVersionInsideGenericArgument)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly]], Test.Other.Assembly");
    const auto expected_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly, Version=11.0.0.0]], Test.Other.Assembly");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, ReplacesLowerVersionInsideGenericArgument)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly, Version=1.0.0.0]], Test.Other.Assembly");
    const auto expected_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly, Version=11.0.0.0]], Test.Other.Assembly");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesEqualVersionInsideGenericArgument)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly, Version=11.0.0.0]], Test.Other.Assembly");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PromotesMapForHigherVersionInsideGenericArgument)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Generic`1[[Test.Type, Test.Redirected.Assembly, Version=12.0.0.0]], Test.Other.Assembly");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesMappedAssembliesAtEveryGenericDepth)
{
    const WSTRING                                           leaf_assembly_name   = WStr("Test.Leaf.Assembly");
    const WSTRING                                           middle_assembly_name = WStr("Test.Middle.Assembly");
    const WSTRING                                           outer_assembly_name  = WStr("Test.Outer.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects            = {{leaf_assembly_name, {11, 1, 2, 3}},
                                                                                    {middle_assembly_name, {12, 0, 0, 0}},
                                                                                    {outer_assembly_name, {13, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Outer`2[[Test.Middle`1[[Test.Leaf, Test.Leaf.Assembly, Version=1.0.0.0]], "
                    "Test.Middle.Assembly],[Test.Unmapped, Test.Unmapped.Assembly]], Test.Outer.Assembly, "
                    "Culture=neutral");
    const auto expected_type_name =
        std::string("Test.Outer`2[[Test.Middle`1[[Test.Leaf, Test.Leaf.Assembly, Version=11.1.2.3]], "
                    "Test.Middle.Assembly, Version=12.0.0.0],[Test.Unmapped, Test.Unmapped.Assembly]], "
                    "Test.Outer.Assembly, Version=13.0.0.0, Culture=neutral");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names,
              (std::vector<WSTRING>{leaf_assembly_name, middle_assembly_name, outer_assembly_name}));
    EXPECT_EQ(redirects.at(leaf_assembly_name).VersionStr(), WStr("11.1.2.3"));
    EXPECT_EQ(redirects.at(middle_assembly_name).VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirects.at(outer_assembly_name).VersionStr(), WStr("13.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesAtMaximumGenericNestingDepth)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        WrapInGenericTypes("Test.Type, Test.Redirected.Assembly, Version=1.0.0.0", max_generic_nesting_depth);
    const auto expected_type_name =
        WrapInGenericTypes("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0", max_generic_nesting_depth);
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, RejectsTypeNameBeyondMaximumGenericNestingDepth)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name =
        WrapInGenericTypes("Test.Type, Test.Redirected.Assembly, Version=1.0.0.0", max_generic_nesting_depth + 1);
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesMixedBracketedAndUnbracketedGenericArguments)
{
    const WSTRING                                           first_assembly_name = WStr("Test.First.Assembly");
    const WSTRING                                           outer_assembly_name = WStr("Test.Outer.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects           = {{first_assembly_name, {11, 0, 0, 0}},
                                                                                   {outer_assembly_name, {12, 0, 0, 0}}};
    const auto                                              input_type_name =
        std::string("Test.Pair`2[[Test.First, Test.First.Assembly, Version=1.0.0.0],Test.Second], Test.Outer.Assembly");
    const auto expected_type_name =
        std::string("Test.Pair`2[[Test.First, Test.First.Assembly, Version=11.0.0.0],Test.Second], "
                    "Test.Outer.Assembly, Version=12.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, (std::vector<WSTRING>{first_assembly_name, outer_assembly_name}));
    EXPECT_EQ(redirects.at(first_assembly_name).VersionStr(), WStr("11.0.0.0"));
    EXPECT_EQ(redirects.at(outer_assembly_name).VersionStr(), WStr("12.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, UsesHigherNestedVersionForEveryReferenceInTheAttribute)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto  input_type_name    = std::string("Test.Pair`2[[Test.Lower, Test.Redirected.Assembly, Version=1.0.0.0],"
                                                     "[Test.Higher, Test.Redirected.Assembly, Version=12.0.0.0]]");
    const auto  expected_type_name = std::string("Test.Pair`2[[Test.Lower, Test.Redirected.Assembly, Version=12.0.0.0],"
                                                  "[Test.Higher, Test.Redirected.Assembly, Version=12.0.0.0]]");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, UsesHigherNestedVersionWhenItAppearsFirst)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto input_type_name    = std::string("Test.Pair`2[[Test.Higher, Test.Redirected.Assembly, Version=12.0.0.0],"
                                                   "[Test.Lower, Test.Redirected.Assembly, Version=1.0.0.0]]");
    const auto expected_type_name = std::string("Test.Pair`2[[Test.Higher, Test.Redirected.Assembly, Version=12.0.0.0],"
                                                "[Test.Lower, Test.Redirected.Assembly, Version=12.0.0.0]]");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, UsesHighestRequestedVersionForRepeatedReferencesInTheAttribute)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto  input_type_name    = std::string("Test.Pair`2[[Test.First, Test.Redirected.Assembly, Version=12.0.0.0],"
                                                     "[Test.Second, Test.Redirected.Assembly, Version=13.0.0.0]]");
    const auto  expected_type_name = std::string("Test.Pair`2[[Test.First, Test.Redirected.Assembly, Version=13.0.0.0],"
                                                  "[Test.Second, Test.Redirected.Assembly, Version=13.0.0.0]]");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("13.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesEveryLowerRepeatedReference)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {3, 0, 0, 0}}};
    const auto  input_type_name    = std::string("Test.Pair`2[[Test.First, Test.Redirected.Assembly, Version=1.0.0.0],"
                                                     "[Test.Second, Test.Redirected.Assembly, Version=2.0.0.0]]");
    const auto  expected_type_name = std::string("Test.Pair`2[[Test.First, Test.Redirected.Assembly, Version=3.0.0.0],"
                                                  "[Test.Second, Test.Redirected.Assembly, Version=3.0.0.0]]");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, (std::vector<WSTRING>{assembly_name, assembly_name}));
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("3.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotTreatUnqualifiedGenericArgumentsAsAssemblies)
{
    const WSTRING                                           unqualified_type_name = WStr("Test.Second");
    const WSTRING                                           outer_assembly_name   = WStr("Test.Outer.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {{unqualified_type_name, {11, 0, 0, 0}},
                                                                         {outer_assembly_name, {11, 0, 0, 0}}};
    const auto input_type_name = std::string("Test.Pair`2[Test.First,Test.Second], Test.Outer.Assembly");
    const auto expected_type_name =
        std::string("Test.Pair`2[Test.First,Test.Second], Test.Outer.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{outer_assembly_name});
    EXPECT_EQ(redirects.at(unqualified_type_name).VersionStr(), WStr("11.0.0.0"));
    EXPECT_EQ(redirects.at(outer_assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesEscapedDelimiters)
{
    const WSTRING                                           assembly_name = WStr("Test,Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto input_type_name = std::string("Test.Type\\,With\\\\Backslash\\[Brackets\\], Test\\,Redirected.Assembly");
    const auto expected_type_name =
        std::string("Test.Type\\,With\\\\Backslash\\[Brackets\\], Test\\,Redirected.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesNonAsciiUtf8BytesAdjacentToDelimiters)
{
    const auto type_name                = std::string("Test.Type") + "\xC3\xB6";
    const auto assembly_name            = std::string("Test.Redirected.Assembly") + "\xC2\xA0";
    const auto input_type_name          = type_name + ", " + assembly_name;
    const auto expected_type_name       = input_type_name + ", Version=11.0.0.0";
    const auto redirected_assembly_name = ToWSTRING(assembly_name);
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {{redirected_assembly_name, {11, 0, 0, 0}}};
    std::string                                             rewritten_type_name;
    std::vector<WSTRING>                                    rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{redirected_assembly_name});
    EXPECT_EQ(redirects.at(redirected_assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesReflectionTypeForms)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto input_type_name = std::string("Test.Outer+Nested*[][*][,]&, Test.Redirected.Assembly");
    const auto expected_type_name =
        std::string("Test.Outer+Nested*[][*][,]&, Test.Redirected.Assembly, Version=11.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteUnmappedAssembly)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto           input_type_name = std::string("Test.Type, Test.Other.Assembly");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteUnqualifiedType)
{
    const WSTRING                                           assembly_name   = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects       = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              input_type_name = std::string("Test.Type");
    std::string                                             rewritten_type_name;
    std::vector<WSTRING>                                    rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesValidShortVersions)
{
    const WSTRING assembly_name = WStr("Test.Redirected.Assembly");
    for (const auto* version : {"9.0", "9.0.1"})
    {
        SCOPED_TRACE(version);
        std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects = {{assembly_name, {11, 0, 0, 0}}};
        const auto           input_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=") + version;
        const auto           expected_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
        std::string          rewritten_type_name;
        std::vector<WSTRING> rewritten_assembly_names;

        ASSERT_TRUE(TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name,
                                                     rewritten_assembly_names));
        EXPECT_EQ(rewritten_type_name, expected_type_name);
        EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
        EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
    }
}

TEST(UnsafeAccessorTypeNameRewriterTest, PreservesEqualShortVersionWhileRewritingLowerReference)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto  input_type_name    = std::string("Test.Pair`2[[Test.Equal, Test.Redirected.Assembly, Version=11.0],"
                                                     "[Test.Lower, Test.Redirected.Assembly, Version=9.0]]");
    const auto  expected_type_name = std::string("Test.Pair`2[[Test.Equal, Test.Redirected.Assembly, Version=11.0],"
                                                  "[Test.Lower, Test.Redirected.Assembly, Version=11.0.0.0]]");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotRewriteMalformedVersions)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    for (const auto* input_type_name :
         {"Test.Type, Test.Redirected.Assembly, Version=11", "Test.Type, Test.Redirected.Assembly, Version=11.0.",
          "Test.Type, Test.Redirected.Assembly, Version=11.0.0.0.0",
          "Test.Type, Test.Redirected.Assembly, Version=11.0.invalid.0",
          "Test.Type, Test.Redirected.Assembly, Version=70000.1.1.1"})
    {
        SCOPED_TRACE(input_type_name);
        std::string          rewritten_type_name;
        std::vector<WSTRING> rewritten_assembly_names;
        EXPECT_FALSE(TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name,
                                                      rewritten_assembly_names));
        EXPECT_TRUE(rewritten_type_name.empty());
        EXPECT_TRUE(rewritten_assembly_names.empty());
        EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
    }
}

TEST(UnsafeAccessorTypeNameRewriterTest, PromotesMapForHigherVersionBeforeFirstRedirection)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto           input_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=12.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("12.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, DoesNotPromoteMapAfterRedirectionWasApplied)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    redirects.at(assembly_name).ulRedirectionCount                        = 1;
    const auto           input_type_name = std::string("Test.Type, Test.Redirected.Assembly, Version=12.0.0.0");
    std::string          rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    EXPECT_FALSE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_type_name.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeNameRewriterTest, RewritesLowerAndPreservesHigherAfterTargetWasCommitted)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {1, 5, 0, 0}}};
    redirects.at(assembly_name).ulRedirectionCount                        = 1;
    const auto  input_type_name    = std::string("Test.Pair`2[[Test.Lower, Test.Redirected.Assembly, Version=1.0.0.0],"
                                                     "[Test.Higher, Test.Redirected.Assembly, Version=2.0.0.0]]");
    const auto  expected_type_name = std::string("Test.Pair`2[[Test.Lower, Test.Redirected.Assembly, Version=1.5.0.0],"
                                                  "[Test.Higher, Test.Redirected.Assembly, Version=2.0.0.0]]");
    std::string rewritten_type_name;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(
        TryRewriteUnsafeAccessorTypeName(input_type_name, redirects, rewritten_type_name, rewritten_assembly_names));
    EXPECT_EQ(rewritten_type_name, expected_type_name);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("1.5.0.0"));
    EXPECT_EQ(redirects.at(assembly_name).ulRedirectionCount, 1);
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWithoutLeavingBytesFromLongerValue)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto original_suffix = std::string(", Test.Redirected.Assembly, Version=1.12345.12345.12345");
    const auto expected_suffix = std::string(", Test.Redirected.Assembly, Version=11.0.0.0");
    const auto type_prefix     = std::string(one_byte_length_max - expected_suffix.size(), 'N');
    const auto original_name   = type_prefix + original_suffix;
    const auto expected_name   = type_prefix + expected_suffix;
    ASSERT_GT(original_name.size(), one_byte_length_max);
    ASSERT_EQ(expected_name.size(), one_byte_length_max);
    const auto           original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto           expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE>    rewritten_blob;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_names));
    EXPECT_EQ(rewritten_blob, expected_blob);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWhenValueNeedsTwoByteLength)
{
    const WSTRING                                           assembly_name   = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects       = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              assembly_suffix = std::string(", Test.Redirected.Assembly");
    const auto type_prefix   = std::string(one_byte_length_max - assembly_suffix.size(), 'N');
    const auto original_name = type_prefix + assembly_suffix;
    const auto expected_name = original_name + ", Version=11.0.0.0";
    ASSERT_EQ(original_name.size(), one_byte_length_max);
    ASSERT_GT(expected_name.size(), one_byte_length_max);
    const auto           original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto           expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE>    rewritten_blob;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_names));
    EXPECT_EQ(rewritten_blob, expected_blob);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RewritesWhenValueNeedsFourByteLength)
{
    const WSTRING                                           assembly_name   = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects       = {{assembly_name, {11, 0, 0, 0}}};
    const auto                                              assembly_suffix = std::string(", Test.Redirected.Assembly");
    const auto type_prefix   = std::string(two_byte_length_max - assembly_suffix.size(), 'N');
    const auto original_name = type_prefix + assembly_suffix;
    const auto expected_name = original_name + ", Version=11.0.0.0";
    ASSERT_EQ(original_name.size(), two_byte_length_max);
    ASSERT_GT(expected_name.size(), two_byte_length_max);
    const auto           original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    const auto           expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    std::vector<BYTE>    rewritten_blob;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_names));
    EXPECT_EQ(rewritten_blob, expected_blob);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, PreservesNamedArguments)
{
    const WSTRING                                           assembly_name = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects     = {{assembly_name, {11, 0, 0, 0}}};
    const auto original_name = std::string("Test.Type, Test.Redirected.Assembly, Version=1.0.0.0");
    const auto expected_name = std::string("Test.Type, Test.Redirected.Assembly, Version=11.0.0.0");
    auto       original_blob = CreateUnsafeAccessorTypeAttributeBlob(original_name);
    auto       expected_blob = CreateUnsafeAccessorTypeAttributeBlob(expected_name);
    // Replace the zero named-argument count with one string property named "Name" whose value is "Value".
    const std::vector<BYTE> named_argument = {0x01, 0x00, 0x54, 0x0E, 0x04, 'N', 'a', 'm',
                                              'e',  0x05, 'V',  'a',  'l',  'u', 'e'};
    original_blob.resize(original_blob.size() - 2);
    expected_blob.resize(expected_blob.size() - 2);
    original_blob.insert(original_blob.end(), named_argument.begin(), named_argument.end());
    expected_blob.insert(expected_blob.end(), named_argument.begin(), named_argument.end());
    std::vector<BYTE>    rewritten_blob;
    std::vector<WSTRING> rewritten_assembly_names;

    ASSERT_TRUE(TryRewriteUnsafeAccessorTypeAttributeBlob(original_blob.data(),
                                                          static_cast<ULONG>(original_blob.size()), redirects,
                                                          rewritten_blob, rewritten_assembly_names));
    EXPECT_EQ(rewritten_blob, expected_blob);
    EXPECT_EQ(rewritten_assembly_names, std::vector<WSTRING>{assembly_name});
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

TEST(UnsafeAccessorTypeAttributeBlobRewriterTest, RejectsTruncatedPackedLength)
{
    const WSTRING                                           assembly_name  = WStr("Test.Redirected.Assembly");
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects      = {{assembly_name, {11, 0, 0, 0}}};
    const std::vector<BYTE>                                 truncated_blob = {0x01, 0x00, 0xC0, 0x00, 0x00};
    std::vector<BYTE>                                       rewritten_blob;
    std::vector<WSTRING>                                    rewritten_assembly_names;

    EXPECT_FALSE(TryRewriteUnsafeAccessorTypeAttributeBlob(truncated_blob.data(),
                                                           static_cast<ULONG>(truncated_blob.size()), redirects,
                                                           rewritten_blob, rewritten_assembly_names));
    EXPECT_TRUE(rewritten_blob.empty());
    EXPECT_TRUE(rewritten_assembly_names.empty());
    EXPECT_EQ(redirects.at(assembly_name).VersionStr(), WStr("11.0.0.0"));
}

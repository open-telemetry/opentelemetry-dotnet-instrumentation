#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/string_utils.h"

using namespace trace;

TEST(StringUtilsTest, ToStringUsesExplicitLength)
{
    const WCHAR value[] = {WStr('a'), WStr('b'), WStr('c'), WStr('d')};

    EXPECT_EQ(ToString(value, 3), "abc");
}

TEST(StringUtilsTest, ToStringPreservesEmbeddedNull)
{
    const WCHAR value[] = {WStr('a'), WStr('\0'), WStr('b')};

    EXPECT_EQ(ToString(value, 3), std::string("a\0b", 3));
}

TEST(StringUtilsTest, ToStringAcceptsEmptyBuffer)
{
    EXPECT_EQ(ToString(nullptr, 0), std::string());
}

#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/string_utils.h"

using namespace trace;

TEST(StringUtilsTest, LongUtf8AndUtf16StringsRoundTrip)
{
    for (const auto length : {511, 512, 513, 1000})
    {
        const WSTRING ascii(length, WStr('a'));
        EXPECT_EQ(ToString(ascii), std::string(length, 'a'));
        EXPECT_EQ(ToWSTRING(std::string(length, 'a')), ascii);

        // UTF-8 needs two bytes per character, so it exceeds the stack buffer before UTF-16 does.
        const WSTRING wide(length, WStr('\u00E9'));
        std::string   utf8;
        for (int i = 0; i < length; i++)
        {
            utf8 += "\xC3\xA9";
        }
        EXPECT_EQ(ToString(wide), utf8);
        EXPECT_EQ(ToWSTRING(utf8), wide);
    }
}

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

TEST(StringUtilsTest, ToStringFormatsGuid)
{
    const GUID guid01 = {0x846f5f1c, 0xf9ae, 0x4b07, {0x96, 0x9e, 0x05, 0xc2, 0x6b, 0xc0, 0x60, 0xd8}};
    const GUID guid02 = {0xbd1a650d, 0xac5d, 0x4896, {0xb6, 0x4f, 0xd6, 0xfa, 0x25, 0xd6, 0xb2, 0x6a}};

    EXPECT_EQ(ToString(guid01), "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}");
    EXPECT_EQ(ToString(guid02), "{BD1A650D-AC5D-4896-B64F-D6FA25D6B26A}");
}

TEST(StringUtilsTest, PadLeft)
{
    EXPECT_EQ(PadLeft("PadLeft", 10), "   PadLeft");
    EXPECT_EQ(PadLeft("PadLeft", 5), "PadLeft");
    EXPECT_EQ(PadLeft("A", 8, '0'), "0000000A");
}

TEST(StringUtilsTest, Hex)
{
    EXPECT_EQ(Hex(0), "0x00000000");
    EXPECT_EQ(Hex(10), "0x0000000A");
    EXPECT_EQ(Hex(static_cast<ULONG>(-1)), "0xFFFFFFFF");
    EXPECT_EQ(Hex(S_FALSE), "0x00000001");
    EXPECT_EQ(Hex(E_FAIL), "0x80004005");
}

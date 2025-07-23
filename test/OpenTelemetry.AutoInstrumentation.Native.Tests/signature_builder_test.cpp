#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/signature_builder.h"
#include <vector>

using namespace trace;

class SignatureBuilderTest : public ::testing::Test
{
};

TEST_F(SignatureBuilderTest, SignatureBuilderDefaultCtor)
{
    SignatureBuilder empty;
    EXPECT_EQ(empty.Size(), 0);
}

TEST_F(SignatureBuilderTest, CorSignatureCtor)
{
    SignatureBuilder for_test{1, 2, 3, 99};
    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({1, 2, 3, 99}));
}

TEST_F(SignatureBuilderTest, PushRawByte)
{
    SignatureBuilder for_test = SignatureBuilder{}.PushRawByte(64).PushRawByte(50);
    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({64, 50}));
}

TEST_F(SignatureBuilderTest, PushRawBytes)
{
    SignatureBuilder for_test = SignatureBuilder{}.PushRawBytes({64, 98}).PushRawBytes({70, 54});
    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({64, 98, 70, 54}));
}

TEST_F(SignatureBuilderTest, PushRawBytesBeginEnd)
{
    std::vector<COR_SIGNATURE> source1{23, 98, 37};
    std::vector<COR_SIGNATURE> source2{35, 42};

    SignatureBuilder for_test =
        SignatureBuilder{}.PushRawBytes(source1.begin(), source1.end()).PushRawBytes(source2.begin(), source2.end());

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({23, 98, 37, 35, 42}));
}

TEST_F(SignatureBuilderTest, PushCompressedData)
{
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(1);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({1}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x7F);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0x7F}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x80);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0x80, 0x80}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x2E57);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0xAE, 0x57}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x3FFF);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0xBF, 0xFF}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x4000);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0xC0, 0x00, 0x40, 0x00}));
    }
    {
        SignatureBuilder for_test = SignatureBuilder{}.PushCompressedData(0x1FFFFFFF);

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({0xDF, 0xFF, 0xFF, 0xFF}));
    }
}

TEST_F(SignatureBuilderTest, PushToken)
{
    SignatureBuilder for_test = SignatureBuilder{}.PushToken(TokenFromRid(0x12, mdtTypeRef));

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({0x49}));
}

TEST_F(SignatureBuilderTest, Push)
{
    SignatureBuilder source1{23, 98, 37};
    SignatureBuilder source2{35, 42};

    SignatureBuilder for_test = SignatureBuilder{}.Push(source1).Push(source2);

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({23, 98, 37, 35, 42}));
}

TEST_F(SignatureBuilderTest, SignatureBuilderCtor)
{
    SignatureBuilder source1{23, 98, 37};
    SignatureBuilder source2{35, 42};

    SignatureBuilder for_test{source1, source2};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({23, 98, 37, 35, 42}));
}

TEST_F(SignatureBuilderTest, CombinationOfApi)
{
    SignatureBuilder source1{23, 98, 37};
    SignatureBuilder source2{35, 42};

    SignatureBuilder for_test = SignatureBuilder{source1, source2}
                                    .PushRawByte(33)
                                    .PushRawBytes({34, 51})
                                    .PushToken(TokenFromRid(0x12, mdtTypeRef))
                                    .PushCompressedData(0x4000);

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({23, 98, 37, 35, 42, 33, 34, 51, 0x49, 0xC0, 0x00, 0x40, 0x00}));
}

TEST_F(SignatureBuilderTest, Type)
{
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Void};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_VOID}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Boolean};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_BOOLEAN}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Char};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_CHAR}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::SByte};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_I1}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Byte};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_U1}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Int16};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_I2}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::UInt16};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_U2}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Int32};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_I4}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::UInt32};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_U4}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Int64};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_I8}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::UInt64};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_U8}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Float};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_R4}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Double};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_R8}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::String};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_STRING}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::IntPtr};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_I}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::UintPtr};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_U}));
    }
    {
        SignatureBuilder::Type for_test{SignatureBuilder::BuiltIn::Object};

        EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
                  std::vector<COR_SIGNATURE>({ELEMENT_TYPE_OBJECT}));
    }
}

TEST_F(SignatureBuilderTest, ValueType)
{
    SignatureBuilder::ValueType for_test{TokenFromRid(0x12, mdtTypeRef)};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_VALUETYPE, 0x49}));
}

TEST_F(SignatureBuilderTest, Class)
{
    SignatureBuilder::Class for_test{TokenFromRid(0x12, mdtTypeRef)};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_CLASS, 0x49}));
}

TEST_F(SignatureBuilderTest, Array)
{
    SignatureBuilder::Array for_test{SignatureBuilder::BuiltIn::String};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_STRING}));
}

TEST_F(SignatureBuilderTest, ByRef)
{
    SignatureBuilder::ByRef for_test{SignatureBuilder::BuiltIn::String};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_BYREF, ELEMENT_TYPE_STRING}));
}

TEST_F(SignatureBuilderTest, GenericArgumentOfMethod)
{
    SignatureBuilder::GenericArgumentOfMethod for_test{0x80};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_MVAR, 0x80, 0x80}));
}

TEST_F(SignatureBuilderTest, GenericArgumentOfType)
{
    SignatureBuilder::GenericArgumentOfType for_test{0x0};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({ELEMENT_TYPE_VAR, 0x00}));
}

TEST_F(SignatureBuilderTest, GenericInstance)
{
    SignatureBuilder::GenericInstance for_test{SignatureBuilder::Class{TokenFromRid(0x12, mdtTypeRef)},
                                               {SignatureBuilder::BuiltIn::String, SignatureBuilder::BuiltIn::Double}};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>(
                  {ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS, 0x49, 2, ELEMENT_TYPE_STRING, ELEMENT_TYPE_R8}));
}

TEST_F(SignatureBuilderTest, Field)
{
    SignatureBuilder::Field for_test{SignatureBuilder::Class{TokenFromRid(0x12, mdtTypeRef)}};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>({IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x49}));
}

TEST_F(SignatureBuilderTest, InstanceMethod)
{
    SignatureBuilder::InstanceMethod for_test{SignatureBuilder::Class{TokenFromRid(0x12, mdtTypeRef)},
                                              {SignatureBuilder::BuiltIn::String, SignatureBuilder::BuiltIn::Double}};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>(
                  {IMAGE_CEE_CS_CALLCONV_HASTHIS, 2, ELEMENT_TYPE_CLASS, 0x49, ELEMENT_TYPE_STRING, ELEMENT_TYPE_R8}));
}

TEST_F(SignatureBuilderTest, StaticMethod)
{
    SignatureBuilder::StaticMethod for_test{SignatureBuilder::Class{TokenFromRid(0x12, mdtTypeRef)},
                                            {SignatureBuilder::BuiltIn::String, SignatureBuilder::BuiltIn::Double}};

    EXPECT_EQ(std::vector<COR_SIGNATURE>(for_test.Head(), for_test.Head() + for_test.Size()),
              std::vector<COR_SIGNATURE>(
                  {IMAGE_CEE_CS_CALLCONV_DEFAULT, 2, ELEMENT_TYPE_CLASS, 0x49, ELEMENT_TYPE_STRING, ELEMENT_TYPE_R8}));
}
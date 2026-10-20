#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_async.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/signature_builder.h"

#include <vector>

using namespace trace;

namespace
{
const mdToken kOuterTypeToken = TokenFromRid(7, mdtTypeRef);
const mdToken kInnerTypeToken = TokenFromRid(9, mdtTypeRef);

std::vector<COR_SIGNATURE> Bytes(const SignatureBuilder& builder)
{
    return {builder.Head(), builder.Head() + builder.Size()};
}

TypeSignature Signature(const std::vector<COR_SIGNATURE>& bytes, ULONG offset = 0)
{
    return {offset, static_cast<ULONG>(bytes.size()) - offset, bytes.data()};
}
} // namespace

TEST(RuntimeAsyncTest, IsMiAsyncMatchesOnlyTheAsyncBit)
{
    EXPECT_EQ(0x2000u, static_cast<unsigned>(miAsync));
    EXPECT_TRUE(IsMiAsync(miAsync));
    EXPECT_TRUE(IsMiAsync(miAsync | miIL | miManaged));
    EXPECT_FALSE(IsMiAsync(0));
    EXPECT_FALSE(IsMiAsync(miIL | miManaged));
    EXPECT_FALSE(IsMiAsync(miInternalCall));
}

TEST(RuntimeAsyncTest, ParsesNonGenericTaskShapes)
{
    for (const auto elementType : {ELEMENT_TYPE_CLASS, ELEMENT_TYPE_VALUETYPE})
    {
        SignatureBuilder builder;
        builder.PushRawByte(elementType).PushToken(kOuterTypeToken);
        const auto bytes = Bytes(builder);

        mdToken openTypeToken = mdTokenNil;
        bool isGenericInst = true;
        bool isValueTypeShape = false;
        TypeSignature typeArg{};

        ASSERT_TRUE(ParseTaskLikeReturnShape(Signature(bytes), openTypeToken, isGenericInst, isValueTypeShape,
                                             typeArg));
        EXPECT_EQ(kOuterTypeToken, openTypeToken);
        EXPECT_FALSE(isGenericInst);
        EXPECT_EQ(elementType == ELEMENT_TYPE_VALUETYPE, isValueTypeShape);
    }
}

TEST(RuntimeAsyncTest, ParsesGenericTaskAndSlicesNestedTypeArgument)
{
    SignatureBuilder builder;
    builder.PushRawByte(ELEMENT_TYPE_GENERICINST)
        .PushRawByte(ELEMENT_TYPE_CLASS)
        .PushToken(kOuterTypeToken)
        .PushCompressedData(1)
        .PushRawByte(ELEMENT_TYPE_GENERICINST)
        .PushRawByte(ELEMENT_TYPE_CLASS)
        .PushToken(kInnerTypeToken)
        .PushCompressedData(1)
        .PushRawByte(ELEMENT_TYPE_STRING);
    const auto bytes = Bytes(builder);

    mdToken openTypeToken = mdTokenNil;
    bool isGenericInst = false;
    bool isValueTypeShape = true;
    TypeSignature typeArg{};

    ASSERT_TRUE(ParseTaskLikeReturnShape(Signature(bytes), openTypeToken, isGenericInst, isValueTypeShape, typeArg));
    EXPECT_EQ(kOuterTypeToken, openTypeToken);
    EXPECT_TRUE(isGenericInst);
    EXPECT_FALSE(isValueTypeShape);
    EXPECT_EQ(bytes.data(), typeArg.pbBase);
    EXPECT_EQ(ELEMENT_TYPE_GENERICINST, typeArg.pbBase[typeArg.offset]);
    EXPECT_EQ(ELEMENT_TYPE_STRING, typeArg.pbBase[typeArg.offset + typeArg.length - 1]);
}

TEST(RuntimeAsyncTest, ParsesMethodGenericTypeArgumentWithBlobOffset)
{
    SignatureBuilder builder;
    builder.PushRawBytes({0, 0, 0})
        .PushRawByte(ELEMENT_TYPE_GENERICINST)
        .PushRawByte(ELEMENT_TYPE_VALUETYPE)
        .PushToken(kOuterTypeToken)
        .PushCompressedData(1)
        .PushRawByte(ELEMENT_TYPE_MVAR)
        .PushCompressedData(0);
    const auto bytes = Bytes(builder);

    mdToken openTypeToken = mdTokenNil;
    bool isGenericInst = false;
    bool isValueTypeShape = false;
    TypeSignature typeArg{};

    ASSERT_TRUE(ParseTaskLikeReturnShape(Signature(bytes, 3), openTypeToken, isGenericInst, isValueTypeShape,
                                         typeArg));
    EXPECT_TRUE(isGenericInst);
    EXPECT_TRUE(isValueTypeShape);
    EXPECT_EQ(bytes.data(), typeArg.pbBase);
    EXPECT_EQ(2u, typeArg.length);
    EXPECT_EQ(ELEMENT_TYPE_MVAR, typeArg.pbBase[typeArg.offset]);
}

TEST(RuntimeAsyncTest, RejectsUnsupportedOrMalformedShapes)
{
    std::vector<std::vector<COR_SIGNATURE>> rejected;

    for (const auto elementType : {ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4, ELEMENT_TYPE_STRING,
                                   ELEMENT_TYPE_OBJECT, ELEMENT_TYPE_SZARRAY, ELEMENT_TYPE_BYREF})
    {
        SignatureBuilder builder;
        builder.PushRawByte(elementType);
        rejected.push_back(Bytes(builder));
    }

    SignatureBuilder wrongArity;
    wrongArity.PushRawByte(ELEMENT_TYPE_GENERICINST)
        .PushRawByte(ELEMENT_TYPE_CLASS)
        .PushToken(kOuterTypeToken)
        .PushCompressedData(2)
        .PushRawByte(ELEMENT_TYPE_I4)
        .PushRawByte(ELEMENT_TYPE_I4);
    rejected.push_back(Bytes(wrongArity));

    rejected.emplace_back(std::initializer_list<COR_SIGNATURE>{ELEMENT_TYPE_CLASS});

    for (const auto& bytes : rejected)
    {
        mdToken openTypeToken = mdTokenNil;
        bool isGenericInst = false;
        bool isValueTypeShape = false;
        TypeSignature typeArg{};

        EXPECT_FALSE(ParseTaskLikeReturnShape(Signature(bytes), openTypeToken, isGenericInst, isValueTypeShape,
                                              typeArg));
    }

    mdToken openTypeToken = mdTokenNil;
    bool isGenericInst = false;
    bool isValueTypeShape = false;
    TypeSignature typeArg{};
    EXPECT_FALSE(ParseTaskLikeReturnShape(TypeSignature{}, openTypeToken, isGenericInst, isValueTypeShape, typeArg));
}

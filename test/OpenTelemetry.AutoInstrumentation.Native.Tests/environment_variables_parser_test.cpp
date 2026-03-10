#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/string_utils.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/environment_variables_parser.h"

using namespace trace;

TEST(TrueConditionTest, TrueValues)
{
    ASSERT_TRUE(TrueCondition(ToWSTRING("true")));
    ASSERT_TRUE(TrueCondition(ToWSTRING("True")));
    ASSERT_TRUE(TrueCondition(ToWSTRING("tRue")));
    ASSERT_TRUE(TrueCondition(ToWSTRING("trUe")));
    ASSERT_TRUE(TrueCondition(ToWSTRING("truE")));
    ASSERT_TRUE(TrueCondition(ToWSTRING("TRUE")));
}

TEST(TrueConditionTest, NonTrueValues)
{
    ASSERT_FALSE(TrueCondition(ToWSTRING("")));
    ASSERT_FALSE(TrueCondition(ToWSTRING("1")));
    ASSERT_FALSE(TrueCondition(ToWSTRING("truee")));
    ASSERT_FALSE(TrueCondition(ToWSTRING("TRUEE")));
}

TEST(FalseConditionTest, FalseValues)
{
    ASSERT_TRUE(FalseCondition(ToWSTRING("false")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("False")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("fAlse")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("faLse")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("falSe")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("falsE")));
    ASSERT_TRUE(FalseCondition(ToWSTRING("FALSE")));
}

TEST(FalseConditionTest, NonFalseValues)
{
    ASSERT_FALSE(FalseCondition(ToWSTRING("")));
    ASSERT_FALSE(FalseCondition(ToWSTRING("0")));
    ASSERT_FALSE(FalseCondition(ToWSTRING("falsee")));
    ASSERT_FALSE(FalseCondition(ToWSTRING("FALSEE")));
}

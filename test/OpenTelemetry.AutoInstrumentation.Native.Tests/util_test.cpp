#ifdef _WIN32
#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/util.h"

using namespace trace;

class UtilTest : public ::testing::Test
{
    void SetUp() override
    {
        SetEnvironmentVariable(WStr("util_test_env_var"), WStr(""));
    }

    void TearDown() override
    {
        SetEnvironmentVariable(WStr("util_test_env_var"), WStr(""));
    }
};
TEST_F(UtilTest, ConfiguredSize)
{
    SetEnvironmentVariable(WStr("util_test_env_var"), WStr("100"));

    const auto configured_value = GetConfiguredSize(WStr("util_test_env_var"), 1024);
    ASSERT_EQ(configured_value, 100);
}

TEST_F(UtilTest, DefaultSize)
{
    SetEnvironmentVariable(WStr("util_test_env_var"), WStr(""));

    const auto configured_value = GetConfiguredSize(WStr("util_test_env_var"), 1024);
    ASSERT_EQ(configured_value, 1024);
}

TEST_F(UtilTest, InvalidTooSmallSize)
{
    SetEnvironmentVariable(WStr("util_test_env_var"), WStr("-1"));

    const auto configured_value = GetConfiguredSize(WStr("util_test_env_var"), 1024);
    ASSERT_EQ(configured_value, 1024);
}

TEST_F(UtilTest, InvalidFormatSize)
{
    SetEnvironmentVariable(WStr("util_test_env_var"), WStr("invalid"));

    const auto configured_value = GetConfiguredSize(WStr("util_test_env_var"), 1024);
    ASSERT_EQ(configured_value, 1024);
}
#endif

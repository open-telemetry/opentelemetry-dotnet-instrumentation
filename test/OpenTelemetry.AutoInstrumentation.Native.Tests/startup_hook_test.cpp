#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/startup_hook.h"

using namespace trace;

TEST(StartupHookTest, GetExpectedStartupHookPathDoesNotAddSeparator) {
  const auto home_path = WStr("C:\\tracer_home\\");
  const auto expected_path = WStr("C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.Instrumentation.StartupHook.dll");

  const auto startup_hook_path = GetExpectedStartupHookPath(home_path);

  ASSERT_EQ(expected_path, startup_hook_path);
}

TEST(StartupHookTest, GetExpectedStartupHookPathAddsSeparator) {
  const auto home_path = WStr("C:\\tracer_home");
  const auto expected_path = WStr("C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.Instrumentation.StartupHook.dll");

  const auto startup_hook_path = GetExpectedStartupHookPath(home_path);

  ASSERT_EQ(expected_path, startup_hook_path);
}

TEST(StartupHookTest, StartupHookIsEnabled) {
  const auto startup_hooks = WStr("C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.Instrumentation.StartupHook.dll");
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_TRUE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenStartupHooksIsEmpty) {
  const auto startup_hooks = WStr("");
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenNoStartupHooksDefined) {
  const WSTRING startup_hooks;
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenStartupHookDoesNotContainOpenTelemetryHook) {
  const auto startup_hooks = WStr("C:\\other_folder\\StartupHook.dll");
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenNotInTheCorrectLocation) {
  const auto startup_hooks = WStr("C:\\other_folder\\netcoreapp3.1\\OpenTelemetry.Instrumentation.StartupHook.dll");
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsEnabledWhenMultipleStartupHooksDefined) {
  std::stringstream ss;
  ss << "C:\\folder1\\StartupHook.dll" << DIR_SEPARATOR
     << "C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.Instrumentation.StartupHook.dll"
     << DIR_SEPARATOR << "C:\\folder2\\StartupHook.dll";

  const auto startup_hooks = ToWSTRING(ss.str());
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_TRUE(is_enabled);
}

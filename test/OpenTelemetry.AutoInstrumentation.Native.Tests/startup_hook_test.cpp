#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/startup_hook.h"

using namespace trace;

TEST(StartupHookTest, StartupHookIsEnabled) {
  const auto startup_hooks =
      std::vector<WSTRING>{WStr("C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.AutoInstrumentation.StartupHook.dll")};
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_TRUE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenStartupHooksIsEmpty) {
  const auto startup_hooks = std::vector<WSTRING>{WStr("")};
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenNoStartupHooksDefined) {
  const std::vector<WSTRING> startup_hooks;
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenStartupHookDoesNotContainOpenTelemetryHook) {
  const auto startup_hooks = std::vector<WSTRING>{WStr("C:\\other_folder\\StartupHook.dll")};
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsNotEnabledWhenNotInTheCorrectLocation) {
  const auto startup_hooks = std::vector<WSTRING>{
      WStr("C:\\other_folder\\netcoreapp3.1\\OpenTelemetry.AutoInstrumentation.StartupHook.dll")};
  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_FALSE(is_enabled);
}

TEST(StartupHookTest, StartupHookIsEnabledWhenMultipleStartupHooksDefined) {
  const auto startup_hooks = std::vector<WSTRING>{
      WStr("C:\\folder1\\StartupHook.dll"),
      WStr("C:\\tracer_home\\netcoreapp3.1\\OpenTelemetry.AutoInstrumentation.StartupHook.dll"),
      WStr("C:\\folder2\\StartupHook.dll"),
  };

  const auto home_path = WStr("C:\\tracer_home");

  const auto is_enabled = IsStartupHookEnabled(startup_hooks, home_path);

  ASSERT_TRUE(is_enabled);
}

#include "pch.h"

#include <algorithm>

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/startup_hook.h"

using namespace trace;

const auto base_path = std::filesystem::path(__FILE__).parent_path();
const auto home_path = std::filesystem::absolute(base_path / ".." / ".." / "bin" / "tracer-home").wstring();
const auto otel_startup_hook_path =
    (std::filesystem::path(home_path) / "netcoreapp3.1" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll").wstring();

TEST(StartupHookTest, StartupHookIsValid) {
  const auto startup_hooks = std::vector<WSTRING>{otel_startup_hook_path};

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_TRUE(is_valid);
}

TEST(StartupHookTest, StartupHookIsNotValidWhenStartupHooksIsEmpty) {
  const auto startup_hooks = std::vector<WSTRING>{WStr("")};

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenNoStartupHooksDefined) {
  const std::vector<WSTRING> startup_hooks;

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenStartupHookDoesNotContainOpenTelemetryHook) {
  const auto startup_hooks = std::vector<WSTRING>{
      (std::filesystem::path(home_path) / "netcoreapp3.1" / "StartupHook.dll").wstring()
  };

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenNotInTheCorrectLocation) {
  const auto startup_hooks = std::vector<WSTRING>{
      (base_path / "other_folder" / "netcoreapp3.1" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll")
          .wstring()
  };

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsValidWhenMultipleStartupHooksDefined) {
  const auto startup_hooks = std::vector<WSTRING>{
      (base_path / "folder1" / "StartupHook.dll").wstring(),
      otel_startup_hook_path,
      (base_path / "folder2" / "StartupHook.dll").wstring(),
  };

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_TRUE(is_valid);
}

#ifdef _WIN32

// Tests mixing Windows folder separators

TEST(StartupHookTest, StartupHookIsValidAltSepOnHomePath) {
  const auto startup_hooks = std::vector<WSTRING>{otel_startup_hook_path};
  auto alt_home_path = home_path;
  std::replace(alt_home_path.begin(), alt_home_path.end(), L'\\', L'/');

  const auto is_valid = IsStartupHookValid(startup_hooks, alt_home_path);

  ASSERT_TRUE(is_valid);
}

TEST(StartupHookTest, StartupHookIsValidAltSepOnStartupHooks) {
  auto alt_startup_hook = otel_startup_hook_path;
  std::replace(alt_startup_hook.begin(), alt_startup_hook.end(), L'\\', L'/');

  const auto startup_hooks = std::vector<WSTRING>{alt_startup_hook};

  const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

  ASSERT_TRUE(is_valid);
}

#endif

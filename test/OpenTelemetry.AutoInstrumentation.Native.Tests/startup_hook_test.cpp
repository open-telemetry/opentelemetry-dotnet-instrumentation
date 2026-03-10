#include "pch.h"

#include <algorithm>

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/startup_hook.h"

using namespace trace;

const auto base_path = std::filesystem::path(__FILE__).parent_path();
const auto home_path = std::filesystem::absolute(base_path / ".." / ".." / "bin" / "tracer-home").wstring();
const auto otel_startup_hook_path =
    (std::filesystem::path(home_path) / "net" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll").wstring();

TEST(StartupHookTest, StartupHookIsValid)
{
    const auto startup_hooks = std::vector<WSTRING>{otel_startup_hook_path};

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_TRUE(is_valid);
}

TEST(StartupHookTest, StartupHookIsNotValidWhenStartupHooksIsEmpty)
{
    const auto startup_hooks = std::vector<WSTRING>{WStr("")};

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenNoStartupHooksDefined)
{
    const std::vector<WSTRING> startup_hooks;

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenStartupHookDoesNotContainOpenTelemetryHook)
{
    const auto startup_hooks =
        std::vector<WSTRING>{(std::filesystem::path(home_path) / "net" / "StartupHook.dll").wstring()};

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsInvalidWhenNotInTheCorrectLocation)
{
    const auto startup_hooks = std::vector<WSTRING>{
        (base_path / "other_folder" / "net" / "OpenTelemetry.AutoInstrumentation.StartupHook.dll").wstring()};

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_FALSE(is_valid);
}

TEST(StartupHookTest, StartupHookIsValidWhenMultipleStartupHooksDefined)
{
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

TEST(StartupHookTest, StartupHookIsValidAltSepOnHomePath)
{
    const auto startup_hooks = std::vector<WSTRING>{otel_startup_hook_path};
    auto       alt_home_path = home_path;
    std::replace(alt_home_path.begin(), alt_home_path.end(), L'\\', L'/');

    const auto is_valid = IsStartupHookValid(startup_hooks, alt_home_path);

    ASSERT_TRUE(is_valid);
}

TEST(StartupHookTest, StartupHookIsValidAltSepOnStartupHooks)
{
    auto alt_startup_hook = otel_startup_hook_path;
    std::replace(alt_startup_hook.begin(), alt_startup_hook.end(), L'\\', L'/');

    const auto startup_hooks = std::vector<WSTRING>{alt_startup_hook};

    const auto is_valid = IsStartupHookValid(startup_hooks, home_path);

    ASSERT_TRUE(is_valid);
}

#endif

TEST(StartupHookTest, GetStartupHookPathReturnsCorrectPath)
{
    // Test for ZIP layout
    auto profiler_path =
        (std::filesystem::path(home_path) / "win-x64" / "OpenTelemetry.AutoInstrumentation.Native.dll").wstring();
    auto startup_hook_path = GetStartupHookPath(profiler_path, EmptyWStr);
    ASSERT_EQ(startup_hook_path, otel_startup_hook_path);

    // Test for NuGet platform dependent layout
    profiler_path =
        (std::filesystem::path(home_path) / "net" / "OpenTelemetry.AutoInstrumentation.Native.dll").wstring();
    startup_hook_path = GetStartupHookPath(profiler_path, EmptyWStr);
    ASSERT_EQ(startup_hook_path, otel_startup_hook_path);

    // Test for NuGet platform independent layout
    profiler_path = (std::filesystem::path(home_path) / "net" / "runtimes" / "win-x64" /
                     "OpenTelemetry.AutoInstrumentation.Native.dll")
                        .wstring();
    startup_hook_path = GetStartupHookPath(profiler_path, EmptyWStr);
    ASSERT_EQ(startup_hook_path, otel_startup_hook_path);

    // Test for OTEL_HOME set
    profiler_path     = (std::filesystem::path(base_path) / "OpenTelemetry.AutoInstrumentation.Native.dll").wstring();
    startup_hook_path = GetStartupHookPath(profiler_path, home_path);
    ASSERT_EQ(startup_hook_path, otel_startup_hook_path);
}

TEST(StartupHookTest, GetStartupHookPathReturnsEmptyWhenNotFound)
{
    // Using "home_path" here would find the DLL as we traverse up to the parent
    // directory, so use a completely unrelated path, e.g. base_path
    auto profiler_path = (std::filesystem::path(base_path) / "OpenTelemetry.AutoInstrumentation.Native.dll").wstring();
    auto startup_hook_path = GetStartupHookPath(profiler_path, EmptyWStr);
    ASSERT_EQ(startup_hook_path, EmptyWStr);
}
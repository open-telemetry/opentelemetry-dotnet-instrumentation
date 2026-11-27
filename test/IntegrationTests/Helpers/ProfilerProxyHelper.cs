// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
namespace IntegrationTests.Helpers;

internal static class ProfilerProxyHelper
{
    public const string ProfilerClsId = "{00000000-0000-0000-0000-000000000001}";

    public static string GetPath()
    {
        var directory = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(), "test", "test-applications", "integrations", "dependency-libs", "TestLibrary.ProfilerProxy.NativeDep", "bin");

        if (EnvironmentTools.IsWindows())
        {
            var platform = EnvironmentTools.GetPlatformDir();
            directory = Path.Combine(directory, EnvironmentTools.GetBuildConfiguration(), platform == "x86" ? "Win32" : platform);
        }

        var extension = EnvironmentTools.IsWindows() ? "dll" : EnvironmentTools.IsMacOS() ? "dylib" : "so";
        var path = Path.Combine(directory, $"TestLibrary.ProfilerProxy.NativeDep.{extension}");
        Assert.True(File.Exists(path), $"Profiler proxy not found at {path}. Build the native test dependencies first.");
        return path;
    }
}
#endif

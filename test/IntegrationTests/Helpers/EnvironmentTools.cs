// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using System.Security.Principal;

namespace IntegrationTests.Helpers;

/// <summary>
/// General use utility methods for all tests and tools.
/// </summary>
public static class EnvironmentTools
{
    public const string ProfilerClsId = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
    public const string DotNetFramework = ".NETFramework";
    public const string CoreFramework = ".NETCoreApp";

    private static readonly Lazy<string> SolutionDirectory = new(() =>
    {
        var startDirectory = Environment.CurrentDirectory;
        var currentDirectory = Directory.GetParent(startDirectory);
        const string searchItem = @"OpenTelemetry.AutoInstrumentation.slnx";

        while (true)
        {
            var solutionFile = currentDirectory?.GetFiles(searchItem).SingleOrDefault();

            if (solutionFile != null)
            {
                break;
            }

            currentDirectory = currentDirectory?.Parent;

            if (currentDirectory == null || !currentDirectory.Exists)
            {
                throw new Exception($"Unable to find solution directory from: {startDirectory}");
            }
        }

        return currentDirectory!.FullName;
    });

    /// <summary>
    /// Find the solution directory from anywhere in the hierarchy.
    /// </summary>
    /// <returns>The solution directory.</returns>
    public static string GetSolutionDirectory()
    {
        return SolutionDirectory.Value;
    }

    public static string GetOS()
    {
        return IsWindows() ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
            string.Empty;
    }

    public static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public static bool IsWindowsAdministrator()
    {
        if (!IsWindows())
        {
            return false;
        }

#pragma warning disable CA1416 // Validate platform compatibility
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    public static string GetPlatform()
    {
        return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    }

    public static bool IsX64()
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.X64;
    }

    public static bool IsArm64()
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    }

    public static string GetPlatformDir()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "ARM64",
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    public static string GetClrProfilerDirectoryName()
    {
        return $"{GetClrProfilerOSDirectoryName()}-{GetPlatformDir().ToLowerInvariant()}";
    }

    private static string? GetClrProfilerOSDirectoryName()
    {
        string? clrProfilerDirectoryName = Environment.GetEnvironmentVariable("OS_TYPE") switch
        {
            "windows" => "win",
            "linux-glibc" => "linux",
            "linux-musl" => "linux-musl",
            "macos" => "osx",
            _ => null
        };

        // If OS_TYPE is null, then fallback to default value.
        if (clrProfilerDirectoryName == null)
        {
            clrProfilerDirectoryName = GetOS() switch
            {
                "win" => "win",
                "linux" => "linux",
                "osx" => "osx",
                _ => null
            };
        }

        return clrProfilerDirectoryName;
    }
}

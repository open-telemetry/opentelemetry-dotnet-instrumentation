// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class BuildTests
{
    [Fact]
    public Task DistributionStructure()
    {
        var distributionFolder = EnvironmentHelper.GetNukeBuildOutput();
        var files = Directory.GetFiles(distributionFolder, "*", SearchOption.AllDirectories);

        var relativesPaths = new List<string>(files.Length);
        foreach (var file in files)
        {
            relativesPaths.Add(file.Substring(distributionFolder.Length));
        }

        relativesPaths.Sort(StringComparer.Ordinal);

        var systemName = GetSystemName();

        return Verifier.Verify(relativesPaths)
            .UseTextForParameters(systemName)
            .DisableDiff();
    }

    private static string GetSystemName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (Environment.GetEnvironmentVariable("IsAlpine") == "true")
            {
                return $"alpine-linux-{GetPlatform()}";
            }

            return $"linux-{GetPlatform()}";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        return "unknown";
    }

    private static string GetPlatform()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException()
        };
    }
}

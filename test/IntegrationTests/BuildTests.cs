// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using IntegrationTests.Helpers;

namespace IntegrationTests;

[UsesVerify]
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
                return "alpine-linux";
            }

            return "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        return "unknown";
    }
}

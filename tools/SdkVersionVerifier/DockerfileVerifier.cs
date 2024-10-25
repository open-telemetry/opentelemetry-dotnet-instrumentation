// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using dockerfile;

namespace SdkVersionVerifier;

internal static partial class DockerfileVerifier
{
    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        var dockerfilesDir = Path.Combine(root, "docker");
        var dockerfiles = Directory.GetFiles(dockerfilesDir, "*.dockerfile");

        return FileVerifier.VerifyMultiple(dockerfiles, VerifyVersionsFromDockerfiles, expectedDotnetSdkVersion);
    }

    [GeneratedRegex(@"-v (\d\.\d\.\d{3})\s", RegexOptions.IgnoreCase, "en-US")]
    internal static partial Regex VersionRegex();

    private static bool VerifyVersionsFromDockerfiles(string content, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        using var stringReader = new StringReader(content);
        var dockerfile = Dockerfile.Parse(stringReader);

        string? net6SdkVersion = null;
        string? net7SdkVersion = null;
        string? net8SdkVersion = null;

        foreach (var instruction in dockerfile.Instructions.Where(i => i.Arguments.Contains("./dotnet-install.sh")))
        {
            // Extract version from line like `&& ./dotnet-install.sh -v 6.0.427 --install-dir /usr/share/dotnet --no-path \`
            var result = VersionRegex().Match(instruction.Arguments);
            if (!result.Success)
            {
                continue;
            }

            var extractedSdkVersion = result.Groups[1].Value;
            if (extractedSdkVersion.StartsWith('6'))
            {
                net6SdkVersion = extractedSdkVersion;
            }
            else if (extractedSdkVersion.StartsWith('7'))
            {
                net7SdkVersion = extractedSdkVersion;
            }
        }

        // Extract NET8 SDK version from the base image tag
        // e.g. FROM mcr.microsoft.com/dotnet/sdk:8.0.403-alpine3.20
        var fromInstruction = dockerfile.Instructions
            .SingleOrDefault(i => i.InstructionName == "FROM" && i.Arguments.StartsWith("mcr.microsoft.com/dotnet/sdk"));

        if (fromInstruction is not null)
        {
            net8SdkVersion = fromInstruction.Arguments.Split(':')[1].Split('-')[0];
        }

        return VersionComparer.CompareVersions(expectedDotnetSdkVersion, net6SdkVersion, net7SdkVersion, net8SdkVersion);
    }
}

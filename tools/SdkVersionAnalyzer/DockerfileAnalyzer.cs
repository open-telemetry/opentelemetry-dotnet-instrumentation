// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using Valleysoft.DockerfileModel;

namespace SdkVersionAnalyzer;

internal static partial class DockerfileAnalyzer
{
    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        return FileAnalyzer.VerifyMultiple(GetDockerfiles(root), VerifySdkVersions, expectedDotnetSdkVersion);
    }

    public static void ModifyVersions(string root, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        FileAnalyzer.ModifyMultiple(GetDockerfiles(root), ModifySdkVersions, requestedDotnetSdkVersion);
    }

    [GeneratedRegex(@"-v (\d\.\d\.\d{3})\s", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex VersionRegex();

    private static string ModifySdkVersions(string content, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        var dockerfile = Dockerfile.Parse(content);
        var runInstruction = GetDotnetInstallingInstruction(dockerfile);

        if (runInstruction is not null)
        {
            runInstruction.Command = GetModifiedInstallCommand(runInstruction.Command, requestedDotnetSdkVersion);
        }

        var fromInstruction = GetFromInstruction(dockerfile);
        var imageName = ImageName.Parse(fromInstruction.ImageName);

        if (IsDotnetSdkImage(imageName))
        {
            fromInstruction.ImageName = GetModifiedImageName(requestedDotnetSdkVersion, imageName);
        }

        return dockerfile.ToString();
    }

    private static bool VerifySdkVersions(string content, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        string? net8SdkVersion = null;
        string? net9SdkVersion = null;
        string? net10SdkVersion = null;

        var dockerfile = Dockerfile.Parse(content);
        var instruction = GetDotnetInstallingInstruction(dockerfile);

        // Extract all the versions from an instruction like:
        // RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
        // && echo "SHA256: $(sha256sum dotnet-install.sh)" \
        //     && echo "de4957e41252191427a8ba0866f640b9f19c98fad62305919de41bd332e9c820  dotnet-install.sh" | sha256sum -c \
        //     && chmod +x ./dotnet-install.sh \
        //     && ./dotnet-install.sh -v 8.0.404 --install-dir /usr/share/dotnet --no-path \
        //     && ./dotnet-install.sh -v 9.0.100 --install-dir /usr/share/dotnet --no-path \
        //     && ./dotnet-install.sh -v 10.0.100 --install-dir /usr/share/dotnet --no-path \
        //     && rm dotnet-install.sh

        if (instruction is not null)
        {
            var matchCollection = VersionRegex().Matches(instruction.ToString());
            foreach (Match match in matchCollection)
            {
                var extractedSdkVersion = match.Groups[1].Value;
                if (VersionComparer.IsNet8Version(extractedSdkVersion))
                {
                    net8SdkVersion = extractedSdkVersion;
                }
                else if (VersionComparer.IsNet9Version(extractedSdkVersion))
                {
                    net9SdkVersion = extractedSdkVersion;
                }
                else if (VersionComparer.IsNet10Version(extractedSdkVersion))
                {
                    net10SdkVersion = extractedSdkVersion;
                }
            }
        }

        // Extract NET8 SDK version from the base image tag
        // e.g. FROM mcr.microsoft.com/dotnet/sdk:8.0.403-alpine3.20

        var fromInstruction = GetFromInstruction(dockerfile);

        var imageName = ImageName.Parse(fromInstruction.ImageName);

        if (IsDotnetSdkImage(imageName))
        {
            var (sdkVersion, _) = GetSdkVersionAndSuffix(imageName);

            if (VersionComparer.IsNet9Version(sdkVersion))
            {
                net9SdkVersion = sdkVersion;
            }
            else if (VersionComparer.IsNet10Version(sdkVersion))
            {
                net10SdkVersion = sdkVersion;
            }
        }

        return VersionComparer.CompareVersions(expectedDotnetSdkVersion, net8SdkVersion, net9SdkVersion, net10SdkVersion);
    }

    private static string GetModifiedImageName(DotnetSdkVersion requestedDotnetSdkVersion, ImageName imageName)
    {
        var (_, suffix) = GetSdkVersionAndSuffix(imageName);
        var modifiedTag = $"{requestedDotnetSdkVersion.Net8SdkVersion}-{suffix}";

        return ImageName.FormatImageName(imageName.Repository, imageName.Registry, modifiedTag, null);
    }

    private static (string SdkVersion, string Suffix) GetSdkVersionAndSuffix(ImageName imageName)
    {
        // Extract sdk version and suffix from a tag like '8.0.403-alpine3.20'
        var parts = imageName.Tag!.Split('-', 2);
        return (parts[0], parts[1]);
    }

    private static bool IsDotnetSdkImage(ImageName imageName)
    {
        return imageName is { Registry: "mcr.microsoft.com", Repository: "dotnet/sdk" };
    }

    private static FromInstruction GetFromInstruction(Dockerfile dockerfile)
    {
        return dockerfile
            .Items
            .OfType<FromInstruction>()
            .First();
    }

    private static Command GetModifiedInstallCommand(Command command, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        var newCommandText = VersionRegex().Replace(command.ToString(), match => $"-v {GetNewVersion(match.Groups[1].Value, requestedDotnetSdkVersion)} ");
        return command.CommandType == CommandType.ShellForm ? ShellFormCommand.Parse(newCommandText) : ExecFormCommand.Parse(newCommandText);
    }

    private static string GetNewVersion(string oldVersion, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        if (VersionComparer.IsNet8Version(oldVersion))
        {
            return requestedDotnetSdkVersion.Net8SdkVersion!;
        }

        if (VersionComparer.IsNet9Version(oldVersion))
        {
            return requestedDotnetSdkVersion.Net9SdkVersion!;
        }

        if (VersionComparer.IsNet10Version(oldVersion))
        {
            return requestedDotnetSdkVersion.Net10SdkVersion!;
        }

        return oldVersion;
    }

    private static string[] GetDockerfiles(string root)
    {
        var dockerfilesDir = Path.Combine(root, "docker");
        return Directory.GetFiles(dockerfilesDir, "*.dockerfile");
    }

    private static RunInstruction? GetDotnetInstallingInstruction(Dockerfile dockerfile)
    {
        return dockerfile.Items.OfType<RunInstruction>().SingleOrDefault(i => i.ToString().Contains("./dotnet-install.sh", StringComparison.Ordinal));
    }
}

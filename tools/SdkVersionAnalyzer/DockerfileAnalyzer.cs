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

    [GeneratedRegex(@"(?:^|-v\s+)(\d+\.\d+\.\d{3}(?:-(?:preview|rc)\.\d+(?:\.\d+)*)?)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex VersionRegex();

    private static string ModifySdkVersions(string content, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        var dockerfile = Dockerfile.Parse(content);
        var runInstruction = GetDotnetInstallingInstruction(dockerfile);

        if (runInstruction?.Command is not null)
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
        string? net10SdkVersion = null;
        string? net11SdkVersion = null;

        var dockerfile = Dockerfile.Parse(content);
        var instruction = GetDotnetInstallingInstruction(dockerfile);

        // Extract all the versions from an instruction like:
        // RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
        // && echo "SHA256: $(sha256sum dotnet-install.sh)" \
        //     && echo "de4957e41252191427a8ba0866f640b9f19c98fad62305919de41bd332e9c820  dotnet-install.sh" | sha256sum -c \
        //     && chmod +x ./dotnet-install.sh \
        //     && ./dotnet-install.sh -v 10.0.100 --install-dir /usr/share/dotnet --no-path \
        //     && ./dotnet-install.sh -v 11.0.100 --install-dir /usr/share/dotnet --no-path \
        //     && rm dotnet-install.sh

        if (instruction is not null)
        {
            var matchCollection = VersionRegex().Matches(instruction.ToString());
            foreach (Match match in matchCollection)
            {
                var extractedSdkVersion = match.Groups[1].Value;
                if (VersionComparer.IsNet10Version(extractedSdkVersion))
                {
                    net10SdkVersion = extractedSdkVersion;
                }
                else if (VersionComparer.IsNet11Version(extractedSdkVersion))
                {
                    net11SdkVersion = extractedSdkVersion;
                }
            }
        }

        // Extract NET11 SDK version from the base image tag
        // e.g. mcr.microsoft.com/dotnet/sdk:11.0.100-preview.4-alpine3.23@sha256:ac4f9f39779acc7af1744324ec24bd50b5ff88fd242325c488df09c7c4579ccf

        var fromInstruction = GetFromInstruction(dockerfile);

        var imageName = ImageName.Parse(fromInstruction.ImageName);

        if (IsDotnetSdkImage(imageName))
        {
            var (sdkVersion, _) = GetSdkVersionAndSuffix(imageName);

            if (VersionComparer.IsNet10Version(sdkVersion))
            {
                net10SdkVersion = sdkVersion;
            }
            else if (VersionComparer.IsNet11Version(sdkVersion))
            {
                net11SdkVersion = sdkVersion;
            }
        }

        return VersionComparer.CompareVersions(expectedDotnetSdkVersion, net10SdkVersion, net11SdkVersion, allowPrereleasePrefix: true);
    }

    private static string GetModifiedImageName(DotnetSdkVersion requestedDotnetSdkVersion, ImageName imageName)
    {
        var (sdkVersion, suffix) = GetSdkVersionAndSuffix(imageName);
        var newVersion = GetNewVersion(sdkVersion, requestedDotnetSdkVersion);
        var modifiedTag = string.IsNullOrEmpty(suffix) ? newVersion : $"{newVersion}-{suffix}";

        return ImageName.FormatImageName(imageName.Repository, imageName.Registry, modifiedTag, null);
    }

    private static (string SdkVersion, string Suffix) GetSdkVersionAndSuffix(ImageName imageName)
    {
        // Extract sdk version and suffix from tags like '11.0.100-preview.4-alpine3.23' or '11.0.100-alpine3.23'.
        var match = VersionRegex().Match(imageName.Tag!);
        if (!match.Success)
        {
            return (imageName.Tag!, string.Empty);
        }

        var suffix = imageName.Tag![match.Length..].TrimStart('-');
        return (match.Groups[1].Value, suffix);
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
        var newCommandText = VersionRegex().Replace(
            command.ToString(),
            match => match.Value.Replace(
                match.Groups[1].Value,
                GetNewVersion(match.Groups[1].Value, requestedDotnetSdkVersion),
                StringComparison.Ordinal));
        return command.CommandType == CommandType.ShellForm ? ShellFormCommand.Parse(newCommandText) : ExecFormCommand.Parse(newCommandText);
    }

    private static string GetNewVersion(string oldVersion, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        if (VersionComparer.IsNet10Version(oldVersion))
        {
            return requestedDotnetSdkVersion.Net10SdkVersion!;
        }

        if (VersionComparer.IsNet11Version(oldVersion))
        {
            return requestedDotnetSdkVersion.Net11SdkVersion!;
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

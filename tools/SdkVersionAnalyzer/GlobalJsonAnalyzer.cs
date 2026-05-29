// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;

namespace SdkVersionAnalyzer;

internal static class GlobalJsonAnalyzer
{
    public static bool VerifyVersions(string root, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        return FileAnalyzer.VerifyMultiple([GetGlobalJsonFile(root)], VerifySdkVersion, expectedDotnetSdkVersion);
    }

    public static void ModifyVersions(string root, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        FileAnalyzer.ModifyMultiple([GetGlobalJsonFile(root)], ModifySdkVersion, requestedDotnetSdkVersion);
    }

    private static bool VerifySdkVersion(string content, DotnetSdkVersion expectedDotnetSdkVersion)
    {
        var sdkVersion = ExtractSdkVersion(content);
        if (sdkVersion is null)
        {
            Console.WriteLine("Unable to extract SDK version from global.json.");
            return false;
        }

        if (VersionComparer.IsNet10Version(sdkVersion))
        {
            return VersionComparer.CompareVersions(expectedDotnetSdkVersion, sdkVersion, null);
        }

        if (VersionComparer.IsNet11Version(sdkVersion))
        {
            return VersionComparer.CompareVersions(expectedDotnetSdkVersion, null, sdkVersion);
        }

        Console.WriteLine($"Unexpected SDK version in global.json: {sdkVersion}");
        return false;
    }

    private static string ModifySdkVersion(string content, DotnetSdkVersion requestedDotnetSdkVersion)
    {
        var rootNode = GetRootNode(content);
        var sdkVersion = ExtractSdkVersion(rootNode);
        if (sdkVersion is null)
        {
            throw new InvalidOperationException("Unable to extract SDK version from global.json.");
        }

        var newSdkVersion = GetNewVersion(sdkVersion, requestedDotnetSdkVersion);
        var sdkNode = rootNode["sdk"]!.AsObject();
        sdkNode["version"] = newSdkVersion;

        return rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
    }

    private static string? ExtractSdkVersion(string content)
    {
        return ExtractSdkVersion(GetRootNode(content));
    }

    private static string? ExtractSdkVersion(JsonNode rootNode)
    {
        return rootNode["sdk"]?["version"]?.GetValue<string>();
    }

    private static JsonNode GetRootNode(string content)
    {
        return JsonNode.Parse(content) ?? throw new InvalidOperationException("global.json must contain a JSON object.");
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

    private static string GetGlobalJsonFile(string root)
    {
        return Path.Combine(root, "global.json");
    }
}

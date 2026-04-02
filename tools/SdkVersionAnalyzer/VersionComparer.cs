// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal static class VersionComparer
{
    public static bool CompareVersions(DotnetSdkVersion expectedDotnetSdkVersion, DotnetSdkVersion version)
    {
        return CompareVersions(expectedDotnetSdkVersion, version.Net8SdkVersion, version.Net9SdkVersion, version.Net10SdkVersion);
    }

    public static bool CompareVersions(
        DotnetSdkVersion expectedDotnetSdkVersion,
        string? net8SdkVersion,
        string? net9SdkVersion,
        string? net10SdkVersion)
    {
        return CompareVersion(expectedDotnetSdkVersion.Net8SdkVersion!, net8SdkVersion) &&
               CompareVersion(expectedDotnetSdkVersion.Net9SdkVersion!, net9SdkVersion) &&
               CompareVersion(expectedDotnetSdkVersion.Net10SdkVersion!, net10SdkVersion);
    }

    public static bool IsNet8Version(string versionString)
    {
        return versionString.StartsWith('8');
    }

    public static bool IsNet9Version(string versionString)
    {
        return versionString.StartsWith('9');
    }

    public static bool IsNet10Version(string versionString)
    {
        return versionString.StartsWith("10", StringComparison.Ordinal);
    }

    private static bool CompareVersion(string expectedVersion, string? extractedVersion)
    {
        // For some of the workflows, singular version of SDK is used
        // e.g. in dotnet-format.yml, so extracted version might be missing.
        if (extractedVersion is not null && extractedVersion != expectedVersion)
        {
            Console.WriteLine($".NET SDK Version mismatch: expected={expectedVersion}, actual={extractedVersion}");
            return false;
        }

        return true;
    }
}

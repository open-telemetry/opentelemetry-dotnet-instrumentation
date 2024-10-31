// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionVerifier;

internal static class VersionComparer
{
    public static bool CompareVersions(DotnetSdkVersion expectedDotnetSdkVersion, DotnetSdkVersion version)
    {
        return CompareVersions(expectedDotnetSdkVersion, version.Net6SdkVersion, version.Net7SdkVersion, version.Net8SdkVersion);
    }

    public static bool CompareVersions(
        DotnetSdkVersion expectedDotnetSdkVersion,
        string? net6SdkVersion,
        string? net7SdkVersion,
        string? net8SdkVersion)
    {
        return CompareVersion(expectedDotnetSdkVersion.Net6SdkVersion!, net6SdkVersion) &&
               CompareVersion(expectedDotnetSdkVersion.Net7SdkVersion!, net7SdkVersion) &&
               CompareVersion(expectedDotnetSdkVersion.Net8SdkVersion!, net8SdkVersion);
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

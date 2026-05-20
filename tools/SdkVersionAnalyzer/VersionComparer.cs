// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal static class VersionComparer
{
    public static bool CompareVersions(DotnetSdkVersion expectedDotnetSdkVersion, DotnetSdkVersion version)
    {
        return CompareVersions(expectedDotnetSdkVersion, version.Net10SdkVersion, version.Net11SdkVersion);
    }

    public static bool CompareVersions(
        DotnetSdkVersion expectedDotnetSdkVersion,
        string? net10SdkVersion,
        string? net11SdkVersion,
        bool allowPrereleasePrefix = false)
    {
        return CompareVersion(expectedDotnetSdkVersion.Net10SdkVersion!, net10SdkVersion, allowPrereleasePrefix) &&
               CompareVersion(expectedDotnetSdkVersion.Net11SdkVersion!, net11SdkVersion, allowPrereleasePrefix);
    }

    public static bool IsNet10Version(string versionString)
    {
        return versionString.StartsWith("10", StringComparison.Ordinal);
    }

    public static bool IsNet11Version(string versionString)
    {
        return versionString.StartsWith("11", StringComparison.Ordinal);
    }

    private static bool CompareVersion(string expectedVersion, string? extractedVersion, bool allowPrereleasePrefix)
    {
        // For some of the workflows, singular version of SDK is used
        // e.g. in dotnet-format.yml, so extracted version might be missing.
        if (extractedVersion is not null && !VersionsMatch(expectedVersion, extractedVersion, allowPrereleasePrefix))
        {
            Console.WriteLine($".NET SDK Version mismatch: expected={expectedVersion}, actual={extractedVersion}");
            return false;
        }

        return true;
    }

    private static bool VersionsMatch(string expectedVersion, string extractedVersion, bool allowPrereleasePrefix)
    {
        if (extractedVersion == expectedVersion)
        {
            return true;
        }

        return allowPrereleasePrefix &&
               IsPrereleaseVersion(extractedVersion) &&
               expectedVersion.StartsWith(extractedVersion, StringComparison.Ordinal) &&
               expectedVersion.Length > extractedVersion.Length &&
               expectedVersion[extractedVersion.Length] == '.';
    }

    private static bool IsPrereleaseVersion(string versionString)
    {
        return versionString.Contains("-preview.", StringComparison.OrdinalIgnoreCase) ||
               versionString.Contains("-rc.", StringComparison.OrdinalIgnoreCase);
    }
}

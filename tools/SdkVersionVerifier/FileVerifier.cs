// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionVerifier;

internal static class FileVerifier
{
    public static bool VerifyMultiple(
        IEnumerable<string> filePaths,
        Func<string, DotnetSdkVersion, bool> versionPredicate,
        DotnetSdkVersion dotnetSdkVersion)
    {
        foreach (var filePath in filePaths)
        {
            Console.WriteLine($"Verifying SDK versions from {filePath}");
            var content = File.ReadAllText(filePath);
            var versionsMatch = versionPredicate(content, dotnetSdkVersion);
            if (versionsMatch)
            {
                continue;
            }

            Console.WriteLine($"Invalid SDK versions in {filePath}");
            return false;
        }

        return true;
    }
}

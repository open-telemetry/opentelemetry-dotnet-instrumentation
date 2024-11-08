// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal static class FileAnalyzer
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

    public static void ModifyMultiple(
        IEnumerable<string> filePaths,
        Func<string, DotnetSdkVersion, string> modifier,
        DotnetSdkVersion dotnetSdkVersion)
    {
        foreach (var filePath in filePaths)
        {
            Console.WriteLine($"Modifying SDK versions in {filePath}");
            var content = File.ReadAllText(filePath);
            var modifiedContent = modifier(content, dotnetSdkVersion);
            File.WriteAllText(filePath, modifiedContent);
        }
    }
}

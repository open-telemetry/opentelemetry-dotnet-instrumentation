// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace GacInstallTool;

internal class Program
{
    private const string InstallFlag = "-i";
    private const string UninstallFlag = "-u";

    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException("Missing arguments. Provide -i path/to/dir to install and -u path/to/dir to uninstall.");
        }

        var flag = args[0];
        var dir = args[1];

        if (flag != InstallFlag && flag != UninstallFlag)
        {
            throw new InvalidOperationException($"Unknown flag: {flag}.");
        }

        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {dir}");
        }

        var install = flag == InstallFlag;
        var publisher = new System.EnterpriseServices.Internal.Publish();
        var files = Directory.GetFiles(dir, "*.dll").ToList();
        var links = Directory.GetFiles(dir, "*.link");
        foreach (var link in links)
        {
            var linkTarget = File.ReadAllText(link);
            files.Add(Path.Combine(dir, "..", linkTarget, Path.GetFileNameWithoutExtension(link)));
        }

        foreach (var file in files)
        {
            // Publisher API call can silently fail.

            if (install)
            {
                publisher.GacInstall(file);
            }
            else
            {
                publisher.GacRemove(file);
            }
        }

        var resultText = install
            ? $"Success: Installed {files.Count} libraries in the GAC."
            : $"Success: Uninstalled {files.Count} libraries from the GAC.";
        Console.WriteLine(resultText);
    }
}

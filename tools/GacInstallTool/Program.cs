// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
        var files = Directory.GetFiles(dir, "*.dll");

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
            ? $"Success: Installed {files.Length} libraries in the GAC."
            : $"Success: Uninstalled {files.Length} libraries from the GAC.";
        Console.WriteLine(resultText);
    }
}

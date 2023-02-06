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
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentNullException(nameof(args));
        }

        var dir = args[0];
        var publisher = new System.EnterpriseServices.Internal.Publish();
        var files = Directory.GetFiles(dir, "*.dll");

        foreach (var file in files)
        {
            // This API call can silently fail.
            publisher.GacInstall(file);
        }

        Console.WriteLine($"Success: Installed {files.Length} libraries in the GAC.");
    }
}

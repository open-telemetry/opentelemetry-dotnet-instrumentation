// <copyright file="RuntimeHelper.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using System.Runtime.Versioning;

namespace IntegrationTests.Helpers;

internal static class RuntimeHelper
{
    public static RuntimeInfo GetCurrentRuntimeInfo()
    {
        var anchorType = typeof(TestHelper);
        var targetFramework = Assembly.GetAssembly(anchorType).GetCustomAttribute<TargetFrameworkAttribute>();

        var parts = targetFramework.FrameworkName.Split(',');
        var runtime = parts[0];
        var isCoreClr = runtime.Equals(EnvironmentTools.CoreFramework);

        var versionParts = parts[1].Replace("Version=v", string.Empty).Split('.');
        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(versionParts[1]);
        var patch = versionParts.Length == 3 ? versionParts[2] : null;

        return new RuntimeInfo()
        {
            TargetFramework = GetTargetFramework(isCoreClr, major, minor, patch),
            IsCoreClr = isCoreClr,
            Major = major,
            Minor = minor,
            Patch = patch,
        };
    }

    private static string GetTargetFramework(bool isCoreClr, int major, int minor, string patch)
    {
        if (isCoreClr)
        {
            if (major >= 5)
            {
                return $"net{major}.{minor}";
            }

            return $"netcoreapp{major}.{minor}";
        }

        return $"net{major}{minor}{patch ?? string.Empty}";
    }

    public class RuntimeInfo
    {
        public string TargetFramework { get; set; }

        public bool IsCoreClr { get; set; }

        public int Major { get; set; }

        public int Minor { get; set; }

        public string Patch { get; set; }
    }
}

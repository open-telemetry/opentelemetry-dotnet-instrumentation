// <copyright file="FrameworkDescription.cs" company="OpenTelemetry Authors">
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

using System;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation;

internal partial class FrameworkDescription
{
    private static readonly ILogger Log = ConsoleLogger.Create(typeof(FrameworkDescription));

    private static readonly Assembly RootAssembly = typeof(object).Assembly;

    private static readonly Tuple<int, string>[] DotNetFrameworkVersionMapping =
    {
        // known min value for each framework version
        Tuple.Create(528040, "4.8"),
        Tuple.Create(461808, "4.7.2"),
        Tuple.Create(461308, "4.7.1"),
        Tuple.Create(460798, "4.7"),
        Tuple.Create(394802, "4.6.2"),
        Tuple.Create(394254, "4.6.1"),
        Tuple.Create(393295, "4.6"),
        Tuple.Create(379893, "4.5.2"),
        Tuple.Create(378675, "4.5.1"),
        Tuple.Create(378389, "4.5"),
    };

    private FrameworkDescription(
        string name,
        string productVersion,
        string osPlatform,
        string osArchitecture,
        string processArchitecture)
    {
        Name = name;
        ProductVersion = productVersion;
        OSPlatform = osPlatform;
        OSArchitecture = osArchitecture;
        ProcessArchitecture = processArchitecture;
    }

    public string Name { get; }

    public string ProductVersion { get; }

    public string OSPlatform { get; }

    public string OSArchitecture { get; }

    public string ProcessArchitecture { get; }

    public override string ToString()
    {
        // examples:
        // .NET Framework 4.8 x86 on Windows x64
        // .NET Core 3.0.0 x64 on Linux x64
        return $"{Name} {ProductVersion} {ProcessArchitecture} on {OSPlatform} {OSArchitecture}";
    }

    private static string GetVersionFromAssemblyAttributes()
    {
        string productVersion = null;

        try
        {
            // if we fail to extract version from assembly path, fall back to the [AssemblyInformationalVersion],
            var informationalVersionAttribute = (AssemblyInformationalVersionAttribute)RootAssembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            // split remove the commit hash from pre-release versions
            productVersion = informationalVersionAttribute?.InformationalVersion?.Split('+')[0];
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting framework version from [AssemblyInformationalVersion]");
        }

        if (productVersion == null)
        {
            try
            {
                // and if that fails, try [AssemblyFileVersion]
                var fileVersionAttribute = (AssemblyFileVersionAttribute)RootAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));
                productVersion = fileVersionAttribute?.Version;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error getting framework version from [AssemblyFileVersion]");
            }
        }

        return productVersion;
    }
}

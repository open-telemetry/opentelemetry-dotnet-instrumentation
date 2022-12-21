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
    private static readonly ILogger Log = OtelLogging.GetLogger();

    private static readonly Assembly RootAssembly = typeof(object).Assembly;

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

    private static string? GetVersionFromAssemblyAttributes()
    {
        string? productVersion = null;

        try
        {
            // if we fail to extract version from assembly path, fall back to the [AssemblyInformationalVersion],
            var informationalVersionAttribute = RootAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

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
                var fileVersionAttribute = RootAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
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

// <copyright file="FrameworkDescription.NetFramework.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using System;
using System.Linq;
using Microsoft.Win32;

namespace OpenTelemetry.AutoInstrumentation
{
    internal partial class FrameworkDescription
    {
        private static FrameworkDescription _instance = null;

        public static FrameworkDescription Instance
        {
            get { return _instance ?? (_instance = Create()); }
        }

        public static FrameworkDescription Create()
        {
            var osArchitecture = "unknown";
            var processArchitecture = "unknown";
            var frameworkVersion = "unknown";

            try
            {
                osArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                processArchitecture = Environment.Is64BitProcess ? "x64" : "x86";
                frameworkVersion = GetNetFrameworkVersion();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting framework description.");
            }

            return new FrameworkDescription(
                name: ".NET Framework",
                productVersion: frameworkVersion,
                osPlatform: "Windows",
                osArchitecture: osArchitecture,
                processArchitecture: processArchitecture);
        }

        private static string GetNetFrameworkVersion()
        {
            string productVersion = null;

            try
            {
                object registryValue;

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    registryValue = subKey?.GetValue("Release");
                }

                if (registryValue is int release)
                {
                    // find the known version on the list with the largest release number
                    // that is lower than or equal to the release number in the Windows Registry
                    productVersion = DotNetFrameworkVersionMapping.FirstOrDefault(t => release >= t.Item1)?.Item2;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error getting .NET Framework version from Windows Registry");
            }

            if (productVersion == null)
            {
                // if we fail to extract version from assembly path,
                // fall back to the [AssemblyInformationalVersion] or [AssemblyFileVersion]
                productVersion = GetVersionFromAssemblyAttributes();
            }

            if (productVersion == null)
            {
                // at this point, everything else has failed (this is probably the same as [AssemblyFileVersion] above)
                productVersion = Environment.Version.ToString();
            }

            return productVersion;
        }
    }
}
#endif

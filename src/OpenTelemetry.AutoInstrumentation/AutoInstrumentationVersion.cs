// <copyright file="AutoInstrumentationVersion.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation;

internal class AutoInstrumentationVersion
{
    public static readonly string Version = GetAssemblyInformationalVersion();

    private static string GetAssemblyInformationalVersion()
    {
        try
        {
            var informationalVersion = typeof(ResourceConfigurator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (informationalVersion == null)
            {
                return string.Empty;
            }

            // informationalVersion could be in the following format:
            // {majorVersion}.{minorVersion}.{patchVersion}.{pre-release label}.{pre-release version}.{gitHeight}.{Git SHA of current commit}
            // The following parts are optional: pre-release label, pre-release version, git height, Git SHA of current commit
            // for example: 1.5.0-alpha.1.40+807f703e1b4d9874a92bd86d9f2d4ebe5b5d52e4

            var indexOfPlusSign = informationalVersion.IndexOf('+');
            return indexOfPlusSign > 0 ? informationalVersion.Substring(0, indexOfPlusSign) : informationalVersion;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

#if NET
            var indexOfPlusSign = informationalVersion.IndexOf('+', StringComparison.Ordinal);
#else
            var indexOfPlusSign = informationalVersion.IndexOf('+');
#endif
            return indexOfPlusSign > 0 ? informationalVersion.Substring(0, indexOfPlusSign) : informationalVersion;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}

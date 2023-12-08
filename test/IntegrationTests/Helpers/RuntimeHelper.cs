// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using Microsoft.Win32;

namespace IntegrationTests.Helpers;

internal static class RuntimeHelper
{
    // https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
    public static string? GetRuntimeVersion()
    {
        const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
        {
            if (ndpKey != null && ndpKey.GetValue("Release") != null)
            {
                return CheckFor45PlusVersion((int)ndpKey.GetValue("Release"));
            }

            return null;
        }

        // Checking the version using >= enables forward compatibility.
        static string CheckFor45PlusVersion(int releaseKey)
        {
            return releaseKey switch
            {
                >= 533320 => "4.8.1+",
                >= 528040 => "4.8",
                >= 461808 => "4.7.2",
                >= 461308 => "4.7.1",
                >= 460798 => "4.7",
                >= 394802 => "4.6.2",
                >= 394254 => "4.6.1",
                >= 393295 => "4.6",
                >= 379893 => "4.5.2",
                >= 378675 => "4.5.1",
                >= 378389 => "4.5",
                _ =>
                    // This code should never execute. A non-null release key should mean
                    // that 4.5 or later is installed.
                    "No 4.5+"
            };
        }
    }
}

#endif

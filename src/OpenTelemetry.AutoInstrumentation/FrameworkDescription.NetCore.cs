// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

internal partial class FrameworkDescription
{
    private static FrameworkDescription? _instance;

    public static FrameworkDescription Instance
    {
        get { return _instance ??= Create(); }
    }

    public static FrameworkDescription Create()
    {
        var frameworkName = "unknown";
        var frameworkVersion = "unknown";
        var osPlatform = "unknown";
        var osArchitecture = "unknown";
        var processArchitecture = "unknown";

        try
        {
            try
            {
                // RuntimeInformation.FrameworkDescription returns a string like ".NET Framework 4.7.2" or ".NET Core 2.1",
                // we want to return everything before the last space
                var frameworkDescription = RuntimeInformation.FrameworkDescription;
                var index = frameworkDescription.LastIndexOf(' ');
                frameworkName = frameworkDescription.Substring(0, index).Trim();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error getting framework name from RuntimeInformation");
            }

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                osPlatform = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                osPlatform = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                osPlatform = "MacOS";
            }

            osArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            frameworkVersion = Environment.Version.ToString();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting framework description.");
        }

        return new FrameworkDescription(
            name: frameworkName,
            productVersion: frameworkVersion,
            osPlatform: osPlatform,
            osArchitecture: osArchitecture,
            processArchitecture: processArchitecture);
    }
}
#endif

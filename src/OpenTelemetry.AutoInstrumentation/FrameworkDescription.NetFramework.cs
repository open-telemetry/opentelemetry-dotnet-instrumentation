// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
using Microsoft.Win32;

namespace OpenTelemetry.AutoInstrumentation;

internal partial class FrameworkDescription
{
    private static readonly Assembly RootAssembly = typeof(object).Assembly;

    private static readonly Tuple<int, string>[] DotNetFrameworkVersionMapping =
    {
        // known min value for each framework version
        Tuple.Create(533320, "4.8.1"),
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

    private static FrameworkDescription? _instance;

    public static FrameworkDescription Instance
    {
        get { return _instance ??= Create(); }
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
        string? productVersion = null;

        try
        {
            object? registryValue;

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
#endif

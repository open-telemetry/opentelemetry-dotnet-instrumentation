// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET || NETCOREAPP
namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Provides access to the Trusted Platform Assemblies (TPA) list.
/// The TPA list is parsed once during static initialization.
/// </summary>
internal static class TrustedPlatformAssembliesHelper
{
    // Trusted Platform Assemblies runtime configuration property
    private const string TrustedPlatformAssembliesPropertyName = "TRUSTED_PLATFORM_ASSEMBLIES";

    /// <summary>
    /// Gets the array of TPA paths. This value is computed once during type initialization.
    /// </summary>
    public static string[] TpaPaths { get; } = ParseTpaPaths();

    public static string? GetAssemblyPath(string assemblyName)
    {
        foreach (var it in TpaPaths)
        {
            if (Path.GetFileNameWithoutExtension(it).Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return it;
            }
        }

        return null;
    }

    private static string[] ParseTpaPaths()
    {
        try
        {
            return ((string)AppContext.GetData(TrustedPlatformAssembliesPropertyName)!)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
        catch
        {
            return [];
        }
    }
}
#endif

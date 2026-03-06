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

    private static string[] ParseTpaPaths()
    {
        try
        {
            return (AppContext.GetData(TrustedPlatformAssembliesPropertyName) as string)?
                .Split(Path.PathSeparator)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray() ?? [];
        }
        catch
        {
            return [];
        }
    }
}
#endif

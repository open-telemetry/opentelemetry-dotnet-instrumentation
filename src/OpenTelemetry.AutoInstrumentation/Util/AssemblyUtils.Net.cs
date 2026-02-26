// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class AssemblyUtils
{
    /// <summary>
    /// Safely retrieves the version of an assembly from a file path
    /// without involving the AssemblyLoadContext or the runtime's assembly loader
    /// </summary>
    /// <remarks>
    /// This method uses low-level System.Reflection.Metadata APIs to read assembly information directly from the
    /// bytes of the file on disk. This approach prevents potential recursion issues
    /// and unnecessary assembly loading that could occur with
    /// <see cref="System.Reflection.AssemblyName.GetAssemblyName(string)"/>.
    /// Use this method when you need to inspect assembly metadata without side effects on the runtime's assembly loading state.
    /// </remarks>
    /// <param name="path">The file path to the assembly.</param>
    /// <returns>The version of the assembly, or null if the assembly cannot be read or does not have metadata.</returns>
    public static Version? GetAssemblyVersionSafe(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new PEReader(stream);

            if (!reader.HasMetadata)
            {
                return null;
            }

            var mr = reader.GetMetadataReader();
            var ad = mr.GetAssemblyDefinition();

            return ad.Version;
        }
        catch
        {
            return null;
        }
    }
}

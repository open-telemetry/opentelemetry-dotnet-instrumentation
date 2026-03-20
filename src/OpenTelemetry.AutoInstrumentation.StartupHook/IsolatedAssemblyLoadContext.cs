// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Custom AssemblyLoadContext for isolated mode.
/// Loads both customer and agent assemblies, picking higher versions,
/// but skipping if the best available version is lower than the requested version.
/// </summary>
internal class IsolatedAssemblyLoadContext()
    : AssemblyLoadContext(StartupHookConstants.IsolatedAssemblyLoadContextName, isCollectible: false)
{
    // TODO in future we may want to have a configuration for this list which will give flexibility for the customer if they know what they are doing;
    // TODO also we can automtically add to excludes, if an assembly failed to load in custom ALC so we won't fail it over and over
    private static readonly Dictionary<int, HashSet<string>> MustUseDefaultAlc = new()
    {
        {
            10,
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Accessibility",
                "Microsoft.Win32.Primitives",
                "Microsoft.Win32.Registry",
                "Microsoft.Win32.SystemEvents",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Collections.Immutable",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel",
                "System.ComponentModel.Annotations",
                "System.ComponentModel.EventBasedAsync",
                "System.ComponentModel.Primitives",
                "System.ComponentModel.TypeConverter",
                "System.Configuration.ConfigurationManager",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.DiagnosticSource",
                "System.Diagnostics.EventLog",
                "System.Diagnostics.FileVersionInfo",
                "System.Diagnostics.Process",
                "System.Diagnostics.StackTrace",
                "System.Diagnostics.TextWriterTraceListener",
                "System.Diagnostics.TraceSource",
                "System.Diagnostics.Tracing",
                "System.Drawing.Common",
                "System.Drawing.Primitives",
                "System.Formats.Asn1",
                "System.Formats.Nrbf",
                "System.IO.Compression",
                "System.IO.Compression.Brotli",
                "System.IO.FileSystem",
                "System.IO.MemoryMappedFiles",
                "System.IO.Pipelines",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Memory",
                "System.Net.Http",
                "System.Net.NameResolution",
                "System.Net.NetworkInformation",
                "System.Net.Primitives",
                "System.Net.Quic",
                "System.Net.Requests",
                "System.Net.Security",
                "System.Net.Sockets",
                "System.Net.WebClient",
                "System.Net.WebHeaderCollection",
                "System.Numerics.Vectors",
                "System.ObjectModel",
                "System.Private.CoreLib",
                "System.Private.Uri",
                "System.Private.Windows.Core",
                "System.Private.Windows.GdiPlus",
                "System.Private.Xml",
                "System.Private.Xml.Linq",
                "System.Reflection.Emit",
                "System.Reflection.Emit.ILGeneration",
                "System.Reflection.Emit.Lightweight",
                "System.Reflection.Metadata",
                "System.Reflection.Primitives",
                "System.Resources.Writer",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Runtime.InteropServices",
                "System.Runtime.InteropServices.RuntimeInformation",
                "System.Runtime.Intrinsics",
                "System.Runtime.Loader",
                "System.Runtime.Numerics",
                "System.Runtime.Serialization.Formatters",
                "System.Security.AccessControl",
                "System.Security.Claims",
                "System.Security.Cryptography",
                "System.Security.Cryptography.ProtectedData",
                "System.Security.Principal.Windows",
                "System.Text.Encoding.CodePages",
                "System.Text.Encoding.Extensions",
                "System.Text.Encodings.Web",
                "System.Text.Json",
                "System.Text.RegularExpressions",
                "System.Threading",
                "System.Threading.AccessControl",
                "System.Threading.Channels",
                "System.Threading.Overlapped",
                "System.Threading.Thread",
                "System.Threading.ThreadPool",
                "System.Threading.Timer",
                "System.Web.HttpUtility",
                "System.Windows.Extensions",
                "System.Windows.Forms",
                "System.Windows.Forms.Primitives",
                "System.Xml.ReaderWriter",
                "System.Xml.XDocument",
                "System.Xml.XmlSerializer"
            }
        },
        {
            9,
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft.Win32.Primitives",
                "Microsoft.Win32.Registry",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Collections.Immutable",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel",
                "System.ComponentModel.Primitives",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.FileVersionInfo",
                "System.Diagnostics.Process",
                "System.Diagnostics.Tracing",
                "System.IO.Compression",
                "System.IO.FileSystem",
                "System.IO.MemoryMappedFiles",
                "System.IO.Pipelines",
                "System.Linq",
                "System.Memory",
                "System.Numerics.Vectors",
                "System.ObjectModel",
                "System.Private.CoreLib",
                "System.Private.Uri",
                "System.Reflection.Emit.ILGeneration",
                "System.Reflection.Emit.Lightweight",
                "System.Reflection.Metadata",
                "System.Reflection.Primitives",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Runtime.InteropServices",
                "System.Runtime.Intrinsics",
                "System.Runtime.Loader",
                "System.Security.AccessControl",
                "System.Security.Claims",
                "System.Security.Principal.Windows",
                "System.Text.Encoding.Extensions",
                "System.Text.Encodings.Web",
                "System.Text.Json",
                "System.Text.RegularExpressions",
                "System.Threading",
                "System.Threading.Thread",
                "System.Threading.ThreadPool",
                "System.Threading.Timer"
            }
        },
        {
            8,
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft.Win32.Primitives",
                "Microsoft.Win32.Registry",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Collections.Immutable",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel",
                "System.ComponentModel.Primitives",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.FileVersionInfo",
                "System.Diagnostics.Process",
                "System.Diagnostics.Tracing",
                "System.IO.Compression",
                "System.IO.FileSystem",
                "System.IO.MemoryMappedFiles",
                "System.IO.Pipelines",
                "System.Linq",
                "System.Memory",
                "System.Numerics.Vectors",
                "System.ObjectModel",
                "System.Private.CoreLib",
                "System.Private.Uri",
                "System.Reflection.Emit.ILGeneration",
                "System.Reflection.Emit.Lightweight",
                "System.Reflection.Metadata",
                "System.Reflection.Primitives",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Runtime.InteropServices",
                "System.Runtime.Intrinsics",
                "System.Runtime.Loader",
                "System.Security.AccessControl",
                "System.Security.Claims",
                "System.Security.Principal.Windows",
                "System.Text.Encoding.Extensions",
                "System.Text.Encodings.Web",
                "System.Text.Json",
                "System.Text.RegularExpressions",
                "System.Threading",
                "System.Threading.Thread",
                "System.Threading.ThreadPool",
                "System.Threading.Timer"
            }
        }
    };

    private static readonly int CurrentRuntimeMajorVersion = GetRuntimeMajorVersion();

    private readonly Dictionary<string, string> _tpaAssemblies = ParseTrustedPlatformAssemblies();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // TODO: temporary no logging here! Logging triggers assembly loads -> infinite recursion.

        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name) || MustUseDefaultAlc[CurrentRuntimeMajorVersion].Contains(name))
        {
            return null;
        }

        // TODO: caching - .NET has an optimization within an ALC and do not call Load() method
        // if a match (same or higher version) is already loaded in current ALC.
        // However, other situations may trigger the Load() for an assembly that is already loaded
        // when a higher version is requested (e.g., programmatic Assembly.Load with an explicit version).
        // In this case caching will help avoiding unnecessary I/O for version check.

        // Find in TPA (customer/runtime assemblies)
        _tpaAssemblies.TryGetValue(name, out var tpaPath);

        // Find in agent assemblies
        var agentPath = ManagedProfilerLocationHelper.GetAssemblyPath(name);

        // Pick higher version (agent wins only if strictly higher)
        var selected = PickHigherVersion(tpaPath, agentPath);
        if (selected == null)
        {
            // TODO: log debug once logging is safe here.
            // This is unexpected assembly so include assebly name an dversion
            return null;
        }

        // Verify that the selected assembly satisfies the requested version.
        // If the best available version is still lower than requested, skip
        // rather than loading a version that's too old.
        if (assemblyName.Version != null)
        {
            try
            {
                var selectedVersion = AssemblyUtils.GetAssemblyVersionSafe(selected);
                if (selectedVersion != null && selectedVersion < assemblyName.Version)
                {
                    // TODO: log warning once logging is safe here.
                    // The warning should include: assembly name, requested version, and best available version.
                    return null;
                }
            }
            catch
            {
                // On error reading version, fall through and attempt to load:
                // by now we have the assembly, located by name in TPA or the agent directory;
                // the only missing piece is a confirmed version.
                // Version mismatches in a correctly built standalone app are not a typical scenario,
                // so skipping here converts a lower-probability uncertainty into a certain fallback
                // to Default ALC, which carries a higher risk of type and state drift than the alternative
                // of loading an assembly at a potentially mismatched version in a scenario
                // that is already documented as unsupported and not typical to reach.
            }
        }

        return LoadFromAssemblyPath(selected);
    }

    private static int GetRuntimeMajorVersion()
    {
        var coreLibAssembly = typeof(object).Assembly;
        var version = coreLibAssembly.GetName().Version
            ?? throw new InvalidOperationException("Unable to determine runtime version");
        return MustUseDefaultAlc.TryAdd(version.Major, [])
            ? throw new InvalidOperationException($"Unsupported runtime version: {version}. No configuration for Default ALC assemblies")
            : version.Major;
    }

    private static Dictionary<string, string> ParseTrustedPlatformAssemblies()
    {
        return TrustedPlatformAssembliesHelper.TpaPaths
            .Select(path => new { Name = Path.GetFileNameWithoutExtension(path), Path = path })
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Name, x => x.Path, StringComparer.OrdinalIgnoreCase);
    }

    private static string? PickHigherVersion(string? tpaPath, string? agentPath)
    {
        if (agentPath == null)
        {
            return tpaPath;
        }

        if (tpaPath == null)
        {
            return agentPath;
        }

        try
        {
            var tpaVersion = AssemblyUtils.GetAssemblyVersionSafe(tpaPath);
            var agentVersion = AssemblyUtils.GetAssemblyVersionSafe(agentPath);

            // TODO we should also check the file version when the assembly versions are the same
            //  e.g. System.Diagnostics.DiagnosticSource from package 10.0.0 and 10.0.2 have the same assembly version

            // Agent wins ONLY if strictly higher
            return agentVersion > tpaVersion ? agentPath : tpaPath;
        }
        catch
        {
            return tpaPath; // On error, prefer TPA
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Custom AssemblyLoadContext for isolated mode.
/// This class should use only primitive types from the list of assemblies that are loaded to Default ALC.
/// More complex logic is delegated to AssemblyLoad which will be suplemented at a later stage
/// </summary>
internal class IsolatedAssemblyLoadContext : AssemblyLoadContext
{
    // TODO in future we may want to have a configuration for this list which will give flexibility for the customer if they know what they are doing;
    // TODO also we can automatically add to excludes, if an assembly failed to load in custom ALC so we won't fail it over and over

    // Assemblies that must be loaded in Default ALC to avoid type identity issues.
    // This minimal set contains only core runtime assemblies that are unavoidable
    // during StartupHook bootstrap before setting up isolation.
    // TODO do we want to raise a warning if we are loading a dll to isolated ALC and it is already loaded to default ALC?
    private static readonly string[] MustUseDefaultAlc = GetRuntimeMajorVersion() switch
    {
        // for .Net 8 and 9 we can allow more aggressive isolation
        < 10 => [
            "System.Private.CoreLib",
            "System.Private.Uri",
            "System.Runtime",
            "System.Runtime.Extensions", // <- TrustedPlatformAssembliesHelper needs Path to look through TPA
            "System.Runtime.Loader" // <- required to instantiate AssemblyLoadContext
        ],
        // for .net 10 we have to let TPA System.Diagnostics.DiagnosticSource to be loaded to Default ALC, so this list is larger
        // https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/4924
        >= 10 => [
            // some assemblies has been resolved only on specific platforms
            // but it's not gonna harm to include them, since we don't expect them to exist on other platforms
            "Accessibility", // [only in: win-arm64; win-x64; win-x86]
            "Microsoft.Win32.Primitives",
            "Microsoft.Win32.Registry",
            "Microsoft.Win32.SystemEvents", // [only in: win-arm64; win-x64; win-x86]
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
            "System.Configuration.ConfigurationManager", // [only in: win-arm64; win-x64; win-x86]
            "System.Console",
            "System.Diagnostics.DiagnosticSource",
            "System.Diagnostics.EventLog", // [only in: win-arm64; win-x64; win-x86]
            "System.Diagnostics.FileVersionInfo",
            "System.Diagnostics.Process",
            "System.Diagnostics.StackTrace",
            "System.Diagnostics.TextWriterTraceListener",
            "System.Diagnostics.TraceSource",
            "System.Diagnostics.Tracing",
            "System.Drawing.Common", // [only in: win-arm64; win-x64; win-x86]
            "System.Drawing.Primitives",
            "System.Formats.Asn1",
            "System.Formats.Nrbf", // [only in: win-arm64; win-x64; win-x86]
            "System.IO.Compression",
            "System.IO.Compression.Brotli",
            "System.IO.MemoryMappedFiles",
            "System.IO.Pipelines",
            "System.IO.Pipes",
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
            "System.Private.Windows.Core", // [only in: win-arm64; win-x64; win-x86]
            "System.Private.Windows.GdiPlus", // [only in: win-arm64; win-x64; win-x86]
            "System.Private.Xml",
            "System.Private.Xml.Linq",
            "System.Reflection.Emit",
            "System.Reflection.Emit.ILGeneration",
            "System.Reflection.Emit.Lightweight",
            "System.Reflection.Metadata",
            "System.Reflection.Primitives",
            "System.Resources.Writer",
            "System.Runtime",
            "System.Runtime.Extensions", // <- TrustedPlatformAssembliesHelper needs Path.PathSeparator to look through TPA
            "System.Runtime.InteropServices",
            "System.Runtime.Intrinsics",
            "System.Runtime.Loader", // <- required to instantiate AssemblyLoadContext
            "System.Runtime.Numerics",
            "System.Runtime.Serialization.Formatters",
            "System.Security.AccessControl",
            "System.Security.Claims",
            "System.Security.Cryptography",
            "System.Security.Cryptography.ProtectedData", // [only in: win-arm64; win-x64; win-x86]
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
            "System.Web.HttpUtility",
            "System.Windows.Extensions", // [only in: win-arm64; win-x64; win-x86]
            "System.Windows.Forms", // [only in: win-arm64; win-x64; win-x86]
            "System.Windows.Forms.Primitives", // [only in: win-arm64; win-x64; win-x86]
            "System.Xml.ReaderWriter",
            "System.Xml.XDocument",
            "System.Xml.XmlSerializer"
        ]
    };

    [ThreadStatic]
    private static AssemblyName? _isLoadingAssemblyName;

    private readonly Func<AssemblyLoadContext, AssemblyName, Assembly?> _assemblyLoad;

    public IsolatedAssemblyLoadContext()
        : base(StartupHookConstants.IsolatedAssemblyLoadContextName, isCollectible: false)
    {
        // switch contextual reflection right away, so that everything that uses reflection
        // during the isolated setup will be redirected and resolved within the isolated ALC
        IsolatedReflectionScope = EnterContextualReflection();
        // load a copy of this assembly to this context to isolate the types
        IsolatedAssembly = LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);
        // load the isolated ALC load helper from isolated ALC and pass it to isolated ALC:
        //  - helper implements the logic of searching both TPA and agent paths and validating compatibility
        //  - init helper through reflection will allow resolving all its dependencies within isolated conetxt
        var isolatedHelperType = IsolatedAssembly.GetType(typeof(IsolatedAssemblyLoadContextHelper).FullName!)!;
        var isolatedHelperDelegateMethod = isolatedHelperType.GetMethod(nameof(IsolatedAssemblyLoadContextHelper.Load), BindingFlags.Static | BindingFlags.Public)!;
        var isolatedHelperDelegate = isolatedHelperDelegateMethod.CreateDelegate<Func<AssemblyLoadContext, AssemblyName, Assembly?>>()!;
        _assemblyLoad = isolatedHelperDelegate;
    }

    public Assembly IsolatedAssembly { get; }

    public ContextualReflectionScope IsolatedReflectionScope { get; }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // nothing we can do
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }

        // skip assemblies that should be loaded to Default ALC
        foreach (var it in MustUseDefaultAlc)
        {
            if (it.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        // delegate loading to the provided AssemblyLoad delegate,
        // if we are not already in the middle of loading an assembly to avoid recursion
        // TODO think about recursion a little bit more
        // TODO think about the fact that ALC should return the same assembly for the same request and how no-recursion logic may affect this
        if (_assemblyLoad is not null && _isLoadingAssemblyName?.Name != assemblyName.Name)
        {
            try
            {
                _isLoadingAssemblyName = assemblyName;
                return _assemblyLoad(this, assemblyName);
            }
            finally
            {
                _isLoadingAssemblyName = null;
            }
        }

        // If we are in a middle of setting up, the requesting assembly can only be
        // a system dependency of the isolation setup and it cannot conflict
        // with agent dependencies, so we just load it from TPA.
        // This fork also covers a recursion of system dependencies
        var tpaPath = TrustedPlatformAssembliesHelper.GetAssemblyPath(assemblyName.Name);

        if (tpaPath is not null)
        {
            return LoadFromAssemblyPath(tpaPath);
        }

        return null;
    }

    private static int GetRuntimeMajorVersion()
    {
        var coreLibAssembly = typeof(object).Assembly;
        var runtimeVersion = coreLibAssembly.GetName().Version
            ?? throw new InvalidOperationException("Unable to determine runtime version");
        return runtimeVersion.Major;
    }
}

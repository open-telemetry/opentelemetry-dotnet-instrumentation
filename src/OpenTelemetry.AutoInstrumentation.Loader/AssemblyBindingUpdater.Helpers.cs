// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
using System.Xml.Linq;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal partial class AssemblyBindingUpdater
{
    private enum CandidateSource
    {
        Profiler,
        CodeBase,
        Probe,
        ExistingRedirect,
    }

    private static Version? TryParseVersion(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return null;
        }

        try
        {
            return new Version(versionString);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ToPublicKeyTokenString(AssemblyName assemblyName)
    {
        var tokenBytes = assemblyName.GetPublicKeyToken();
        if (tokenBytes == null || tokenBytes.Length == 0)
        {
            return string.Empty;
        }

#pragma warning disable CA1308
        return BitConverter.ToString(tokenBytes).ToLowerInvariant().Replace("-", string.Empty);
#pragma warning restore CA1308
    }

    private static XElement CreateDependentAssembly(AssemblyCatalog.AssemblyInfo assemblyInfo)
        => new(
            Names.DependentAssembly,
            new XElement(
                Names.AssemblyIdentity,
                new XAttribute(Names.Name, assemblyInfo.FullName.Name),
                new XAttribute(Names.PublicKeyToken, assemblyInfo.Token),
                new XAttribute(Names.Culture, Names.NeutralCulture)));

    private static class Names
    {
        public const string NeutralCulture = "neutral";
        public static readonly XName Configuration = XName.Get("configuration");
        public static readonly XName Runtime = XName.Get("runtime");
        public static readonly XName AssemblyBinding = XName.Get("assemblyBinding", AsmNs);
        public static readonly XName AssemblyIdentity = XName.Get("assemblyIdentity", AsmNs);
        public static readonly XName BindingRedirect = XName.Get("bindingRedirect", AsmNs);
        public static readonly XName DependentAssembly = XName.Get("dependentAssembly", AsmNs);
        public static readonly XName CodeBase = XName.Get("codeBase", AsmNs);
        public static readonly XName Name = XName.Get("name");
        public static readonly XName PublicKeyToken = XName.Get("publicKeyToken");
        public static readonly XName NewVersion = XName.Get("newVersion");
        public static readonly XName OldVersion = XName.Get("oldVersion");
        public static readonly XName Culture = XName.Get("culture");
        public static readonly XName Version = XName.Get("version");
        public static readonly XName Href = XName.Get("href");
        public static readonly XName Probing = XName.Get("probing", AsmNs);
        public static readonly XName PrivatePath = XName.Get("privatePath");
        private const string AsmNs = "urn:schemas-microsoft-com:asm.v1";
    }

    private sealed class AssemblyCandidate
    {
        internal AssemblyCandidate(
            CandidateSource source,
            Version assemblyVersion,
            Version? fileVersion = null,
            string? codeBaseUri = null)
        {
            Source = source;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            CodeBaseUri = codeBaseUri;
        }

        public CandidateSource Source { get; }

        public Version AssemblyVersion { get; }

        public Version? FileVersion { get; }

        public string? CodeBaseUri { get; }

        public bool HasCodeBase => !string.IsNullOrWhiteSpace(CodeBaseUri);
    }
}
#endif

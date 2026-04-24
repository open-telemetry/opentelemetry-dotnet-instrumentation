// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class AssemblyBindingUpdater(IOtelLogger logger, AssemblyCatalog assemblyCatalog)
{
    /// <summary>
    /// Modify assembly bindings in appDomainSetup
    /// </summary>
    /// <param name="appDomainSetup">appDomainSetup to be updated</param>
    internal void ModifyAssemblyRedirectConfig(AppDomainSetup appDomainSetup)
    {
        static XElement ResolveOrAddAssemblyBinding(XDocument config)
        {
            var configuration = config.Element(Names.Configuration);
            if (configuration == null)
            {
                throw new InvalidOperationException();
            }

            var runtime = configuration.Element(Names.Runtime);
            if (runtime == null)
            {
                runtime = new XElement(Names.Runtime);
                configuration.Add(runtime);
            }

            var assemblyBinding = runtime.Element(Names.AssemblyBinding);
            if (assemblyBinding == null)
            {
                assemblyBinding = new XElement(Names.AssemblyBinding);
                runtime.Add(assemblyBinding);
            }

            return assemblyBinding;
        }

        try
        {
            XDocument? config = null;

            // First, try to get configuration from bytes
            var configBytes = appDomainSetup.GetConfigurationBytes();
            if (configBytes != null && configBytes.Length > 0)
            {
                logger.Debug("Loading config from GetConfigurationBytes");
                using var memoryStream = new MemoryStream(configBytes);
                config = XDocument.Load(memoryStream);
            }
            else
            {
                var configPath = appDomainSetup.ConfigurationFile;
                if (!string.IsNullOrEmpty(configPath))
                {
                    try
                    {
                        logger.Debug($"Try to modify {configPath}");
                        config = XDocument.Load(configPath);
                    }
                    catch (Exception)
                    {
                        logger.Debug($"Failed to load {configPath}, new config will be used instead");
                    }
                }

                if (config == null)
                {
                    logger.Debug("Creating new config document");
                    config = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(Names.Configuration));
                }
            }

            var assemblyBinding = ResolveOrAddAssemblyBinding(config);

            ModifyAssemblyBinding(assemblyBinding, appDomainSetup);

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8 };
            using var outputStream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(outputStream, settings);
            config.WriteTo(xmlWriter);
            xmlWriter.Flush();
            appDomainSetup.SetConfigurationBytes(outputStream.ToArray());
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Debug($"Config modified: {assemblyBinding}");
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to modify app domain config ");
        }
    }

    private void ModifyAssemblyBinding(XElement assemblyBinding, AppDomainSetup appDomainSetup)
    {
        static bool MatchesAssembly(XElement dependentAssembly, AssemblyCatalog.AssemblyInfo assemblyInfo)
        {
            var identity = dependentAssembly.Element(Names.AssemblyIdentity);
            if (identity == null)
            {
                return false;
            }

            return string.Equals(identity.Attribute(Names.Name)?.Value, assemblyInfo.FullName.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(identity.Attribute(Names.PublicKeyToken)?.Value, assemblyInfo.Token, StringComparison.OrdinalIgnoreCase);
        }

        var probeDirectories = GetProbeDirectories(assemblyBinding, appDomainSetup);
        var disallowBindingRedirects = appDomainSetup.DisallowBindingRedirects;
        if (disallowBindingRedirects)
        {
            logger.Warning("AppDomainSetup.DisallowBindingRedirects is set. Binding redirects will not be modified; only missing codeBase entries will be added.");
        }

        foreach (var assemblyInfo in assemblyCatalog.GetAssemblies())
        {
            var matchingAssemblies = assemblyBinding
                .Elements(Names.DependentAssembly)
                .Where(dependentAssembly => MatchesAssembly(dependentAssembly, assemblyInfo))
                .ToList();
            var existingRedirects = matchingAssemblies
                .Elements(Names.BindingRedirect)
                .ToList();
            var effectiveRedirectVersion = TryResolveEffectiveRedirectVersion(existingRedirects, assemblyInfo);
            var candidates = CollectCandidates(assemblyInfo, matchingAssemblies, effectiveRedirectVersion, probeDirectories);

            if (disallowBindingRedirects)
            {
                EnsureCandidateCodeBases(assemblyBinding, matchingAssemblies, assemblyInfo, candidates);
                continue;
            }

            if (existingRedirects.Count > 1 && effectiveRedirectVersion == null)
            {
                logger.Warning($"Multiple redirections for {assemblyInfo.FullName.Name} found. Redirect update skipped.");
                EnsureCandidateCodeBases(assemblyBinding, matchingAssemblies, assemblyInfo, candidates);
                continue;
            }

            var selectedCandidate = SelectCandidate(assemblyInfo, candidates);
            logger.Debug($"Selected {selectedCandidate.Source} candidate {selectedCandidate.AssemblyVersion} for {assemblyInfo.FullName.Name}");

            RewriteDependentAssembly(
                assemblyBinding,
                matchingAssemblies,
                assemblyInfo,
                selectedCandidate);
        }
    }

    private List<AssemblyCandidate> CollectCandidates(
        AssemblyCatalog.AssemblyInfo assemblyInfo,
        List<XElement> matchingAssemblies,
        Version? effectiveRedirectVersion,
        List<string> probeDirectories)
    {
        static string DescribeCandidate(AssemblyCandidate candidate)
            => $"{candidate.Source}:{candidate.AssemblyVersion}" +
               (candidate.FileVersion == null ? string.Empty : $"/{candidate.FileVersion}");

        var candidates = new List<AssemblyCandidate>
        {
            new(
                CandidateSource.Profiler,
                assemblyInfo.Version,
                assemblyInfo.FileVersion,
                new Uri(assemblyInfo.Path).AbsoluteUri),
        };

        foreach (var codeBase in matchingAssemblies.Elements(Names.CodeBase))
        {
            var version = TryParseVersion(codeBase.Attribute(Names.Version)?.Value);
            var href = codeBase.Attribute(Names.Href)?.Value;
            if (version == null || href == null || string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            candidates.Add(new(CandidateSource.CodeBase, version, codeBaseUri: href));
        }

        var probingCandidate = TryResolveProbingCandidate(probeDirectories, assemblyInfo);
        if (probingCandidate != null)
        {
            candidates.Add(probingCandidate);
        }

        if (effectiveRedirectVersion != null)
        {
            candidates.Add(new(CandidateSource.ExistingRedirect, effectiveRedirectVersion));
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Debug($"Collected candidates for {assemblyInfo.FullName.Name}: {string.Join(", ", candidates.Select(DescribeCandidate))}");
        }

        return candidates;
    }

#pragma warning disable SA1204
    private static void EnsureCandidateCodeBases(
        XElement assemblyBinding,
        List<XElement> matchingAssemblies,
        AssemblyCatalog.AssemblyInfo assemblyInfo,
        List<AssemblyCandidate> candidates)
    {
        var dependentAssembly = matchingAssemblies.FirstOrDefault();
        if (dependentAssembly == null)
        {
            dependentAssembly = CreateDependentAssembly(assemblyInfo);
            assemblyBinding.Add(dependentAssembly);
        }

        var existingVersions = new HashSet<string>(
            matchingAssemblies
                .Elements(Names.CodeBase)
                .Select(codeBase => codeBase.Attribute(Names.Version)?.Value)
                .OfType<string>()
                .Where(version => !string.IsNullOrWhiteSpace(version)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates.Where(candidate => candidate.HasCodeBase))
        {
            var versionString = candidate.AssemblyVersion.ToString();
            if (existingVersions.Contains(versionString))
            {
                continue;
            }

            dependentAssembly.Add(
                new XElement(
                    Names.CodeBase,
                    new XAttribute(Names.Version, candidate.AssemblyVersion),
                    new XAttribute(Names.Href, candidate.CodeBaseUri)));
            existingVersions.Add(versionString);
        }
    }

    private AssemblyCandidate SelectCandidate(AssemblyCatalog.AssemblyInfo assemblyInfo, List<AssemblyCandidate> candidates)
    {
        var existingRedirect = candidates
            .Where(candidate => candidate.Source == CandidateSource.ExistingRedirect)
            .OrderByDescending(candidate => candidate.AssemblyVersion)
            .FirstOrDefault();
        var highestNonRedirectVersion = candidates
            .Where(candidate => candidate.Source != CandidateSource.ExistingRedirect)
            .Select(candidate => candidate.AssemblyVersion)
            .DefaultIfEmpty(new Version(0, 0, 0, 0))
            .Max();

        if (existingRedirect != null && existingRedirect.AssemblyVersion > highestNonRedirectVersion)
        {
            logger.Information($"Existing redirect for {assemblyInfo.FullName.Name} already targets higher version {existingRedirect.AssemblyVersion}. It will be preserved.");
            return existingRedirect;
        }

        var topCandidates = candidates
            .Where(candidate => candidate.AssemblyVersion == highestNonRedirectVersion)
            .ToList();

        var codeBaseCandidate = topCandidates.FirstOrDefault(candidate => candidate.Source == CandidateSource.CodeBase);
        if (codeBaseCandidate != null)
        {
            return codeBaseCandidate;
        }

        var profilerCandidate = topCandidates.FirstOrDefault(candidate => candidate.Source == CandidateSource.Profiler);
        var probingCandidate = topCandidates.FirstOrDefault(candidate => candidate.Source == CandidateSource.Probe);
        if (profilerCandidate != null)
        {
            if (probingCandidate != null &&
                profilerCandidate.FileVersion != null &&
                probingCandidate.FileVersion != null &&
                profilerCandidate.FileVersion != probingCandidate.FileVersion)
            {
                var selected = profilerCandidate.FileVersion > probingCandidate.FileVersion
                    ? profilerCandidate
                    : probingCandidate;
                logger.Warning($"File version comparison selected {selected.Source} candidate for {assemblyInfo.FullName.Name}: profiler={profilerCandidate.FileVersion}, probe={probingCandidate.FileVersion}");
                return selected;
            }

            return profilerCandidate;
        }

        if (probingCandidate != null)
        {
            return probingCandidate;
        }

        throw new InvalidOperationException($"No selectable candidate found for {assemblyInfo.FullName.Name}. We should never reach this point because at least the profiler candidate should always be present.");
    }

    private static void RewriteDependentAssembly(
        XElement assemblyBinding,
        List<XElement> matchingAssemblies,
        AssemblyCatalog.AssemblyInfo assemblyInfo,
        AssemblyCandidate selectedCandidate)
    {
        foreach (var dependentAssembly in matchingAssemblies)
        {
            dependentAssembly.Remove();
        }

        var rewrittenDependentAssembly = CreateDependentAssembly(assemblyInfo);
        rewrittenDependentAssembly.Add(
            new XElement(
                Names.BindingRedirect,
                new XAttribute(Names.OldVersion, $"0.0.0.0-{selectedCandidate.AssemblyVersion}"),
                new XAttribute(Names.NewVersion, selectedCandidate.AssemblyVersion)));

        if (selectedCandidate.HasCodeBase)
        {
            rewrittenDependentAssembly.Add(
                new XElement(
                    Names.CodeBase,
                    new XAttribute(Names.Version, selectedCandidate.AssemblyVersion),
                    new XAttribute(Names.Href, selectedCandidate.CodeBaseUri)));
        }

        assemblyBinding.Add(rewrittenDependentAssembly);
    }

    private AssemblyCandidate? TryResolveProbingCandidate(List<string> probeDirectories, AssemblyCatalog.AssemblyInfo assemblyInfo)
    {
        static IEnumerable<string> EnumerateProbeCandidatePaths(string probeDirectory, string assemblyName)
        {
            yield return Path.Combine(probeDirectory, $"{assemblyName}.dll");
            yield return Path.Combine(probeDirectory, assemblyName, $"{assemblyName}.dll");
        }

        foreach (var probeDirectory in probeDirectories)
        {
            // We only probe for culture-neutral profiler dependencies, so culture-specific folders are
            // intentionally not modeled here. We also skip the CLR's .exe fallback because our managed
            // profiler dependencies are expected to be assemblies we ship as .dll files.
            foreach (var candidatePath in EnumerateProbeCandidatePaths(probeDirectory, assemblyInfo.FullName.Name))
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                logger.Debug($"Discovered probing candidate for {assemblyInfo.FullName.Name}: {candidatePath}");

                // Match .NET Framework probing: once a file exists at a probed path, the bind
                // does not continue to later paths if that file has the wrong identity or cannot be inspected.
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(candidatePath);
                    var token = ToPublicKeyTokenString(assemblyName);
                    if (!string.Equals(assemblyName.Name, assemblyInfo.FullName.Name, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(token, assemblyInfo.Token, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Debug($"Ignoring first probing hit for {assemblyInfo.FullName.Name} at {candidatePath} because identity does not match.");
                        return null;
                    }

                    if (assemblyName.Version == null)
                    {
                        return null;
                    }

                    return new(CandidateSource.Probe, assemblyName.Version, ReadFileVersion(candidatePath), new Uri(candidatePath).AbsoluteUri);
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, $"Failed to inspect probing candidate {candidatePath} for {assemblyInfo.FullName.Name}");
                    return null;
                }
            }
        }

        logger.Debug($"No probing candidate found for {assemblyInfo.FullName.Name}.");
        return null;
    }

    private List<string> GetProbeDirectories(XElement assemblyBinding, AppDomainSetup appDomainSetup)
    {
        static bool IsWebApplication(AppDomainSetup appDomainSetup)
            => string.Equals(Path.GetFileName(appDomainSetup.ConfigurationFile), "web.config", StringComparison.OrdinalIgnoreCase);

        static string FormatProbeDirectoryResolutionMessage(
            AppDomainSetup appDomainSetup,
            string? applicationBase,
            bool disableApplicationBaseProbe,
            IReadOnlyCollection<string> directories)
        {
            var context = new List<string>();

            if (!string.IsNullOrWhiteSpace(appDomainSetup.ConfigurationFile))
            {
                context.Add($"configurationFile='{appDomainSetup.ConfigurationFile}'");
            }

            if (!string.IsNullOrWhiteSpace(applicationBase))
            {
                context.Add($"applicationBase='{applicationBase}'");
            }

            if (appDomainSetup.DisallowApplicationBaseProbing)
            {
                context.Add("disallowApplicationBaseProbing=True");
            }

            if (disableApplicationBaseProbe)
            {
                context.Add("disableApplicationBaseProbe=True");
            }

            var prefix = "Resolved probing directories for assembly binding update";
            if (context.Count > 0)
            {
                prefix += $" ({string.Join(", ", context)})";
            }

            return $"{prefix}: directories=[{string.Join(", ", directories)}]";
        }

        string? ResolveApplicationBase()
        {
            if (!string.IsNullOrWhiteSpace(appDomainSetup.ApplicationBase))
            {
                return appDomainSetup.ApplicationBase;
            }

            var creatorApplicationBase = GetCreatorApplicationBase();
            if (!string.IsNullOrWhiteSpace(creatorApplicationBase))
            {
                appDomainSetup.ApplicationBase = creatorApplicationBase;
                logger.Debug($"AppDomainSetup.ApplicationBase is not set. Falling back to creator AppDomain base '{creatorApplicationBase}'.");
            }

            return creatorApplicationBase;
        }

        List<string> ResolveProbeDirectories(string applicationBase, string privatePaths)
        {
            static bool IsPathUnderApplicationBase(string applicationBase, string path)
            {
                var normalizedApplicationBase = Path.GetFullPath(applicationBase).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return normalizedPath.Equals(normalizedApplicationBase, StringComparison.OrdinalIgnoreCase)
                    || normalizedPath.StartsWith(normalizedApplicationBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }

            var directories = new List<string>();
            foreach (var privatePath in privatePaths.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedPath = privatePath.Trim();
                if (trimmedPath.Length == 0)
                {
                    continue;
                }

                if (Path.IsPathRooted(trimmedPath))
                {
                    var fullPath = Path.GetFullPath(trimmedPath);
                    if (IsPathUnderApplicationBase(applicationBase, fullPath))
                    {
                        directories.Add(fullPath);
                    }
                    else
                    {
                        logger.Warning($"Ignoring rooted probing path '{trimmedPath}' because it is outside AppDomainSetup.ApplicationBase '{applicationBase}'.");
                    }
                }
                else
                {
                    directories.Add(Path.Combine(applicationBase, trimmedPath));
                }
            }

            return directories;
        }

        static IEnumerable<string> DistinctDirectories(IEnumerable<string> directories)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in directories)
            {
                var fullPath = Path.GetFullPath(directory);
                if (seen.Add(fullPath))
                {
                    yield return fullPath;
                }
            }
        }

        if (appDomainSetup.DisallowApplicationBaseProbing)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Debug(
                    FormatProbeDirectoryResolutionMessage(
                        appDomainSetup,
                        applicationBase: null,
                        disableApplicationBaseProbe: false,
                        directories: []));
            }

            return [];
        }

        var applicationBase = ResolveApplicationBase() ?? string.Empty;
        if (applicationBase.Length == 0)
        {
            var configurationFile = appDomainSetup.ConfigurationFile;
            var context = string.IsNullOrWhiteSpace(configurationFile)
                ? string.Empty
                : $" (configurationFile='{configurationFile}')";
            logger.Warning($"AppDomainSetup.ApplicationBase could not be resolved{context}. Probing for assembly binding update will be skipped.");
            return [];
        }

        var disableApplicationBaseProbe = appDomainSetup.PrivateBinPathProbe != null;
        var directories = new List<string>();

        if (!disableApplicationBaseProbe)
        {
            directories.Add(applicationBase);
        }

        if (IsWebApplication(appDomainSetup))
        {
            directories.Add(Path.Combine(applicationBase, "bin"));
        }

        var privateBinPath = appDomainSetup.PrivateBinPath;
        if (privateBinPath != null && !string.IsNullOrWhiteSpace(privateBinPath))
        {
            directories.AddRange(ResolveProbeDirectories(applicationBase, privateBinPath));
        }

        foreach (var probing in assemblyBinding.Elements(Names.Probing))
        {
            var privatePath = probing.Attribute(Names.PrivatePath)?.Value;
            if (privatePath != null && !string.IsNullOrWhiteSpace(privatePath))
            {
                directories.AddRange(ResolveProbeDirectories(applicationBase, privatePath));
            }
        }

        var resolvedDirectories = DistinctDirectories(directories).ToList();
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Debug(
                FormatProbeDirectoryResolutionMessage(appDomainSetup, applicationBase, disableApplicationBaseProbe, resolvedDirectories));
        }

        return resolvedDirectories;
    }

#pragma warning disable SA1202
    protected virtual string? GetCreatorApplicationBase()
        => AppDomain.CurrentDomain.BaseDirectory;
#pragma warning restore SA1202

    private static XElement CreateDependentAssembly(AssemblyCatalog.AssemblyInfo assemblyInfo)
        => new(
            Names.DependentAssembly,
            new XElement(
                Names.AssemblyIdentity,
                new XAttribute(Names.Name, assemblyInfo.FullName.Name),
                new XAttribute(Names.PublicKeyToken, assemblyInfo.Token),
                new XAttribute(Names.Culture, Names.NeutralCulture)));

    private Version? TryResolveEffectiveRedirectVersion(List<XElement> existingRedirects, AssemblyCatalog.AssemblyInfo assemblyInfo)
    {
        if (existingRedirects.Count == 0)
        {
            return null;
        }

        if (existingRedirects.Count == 1)
        {
            return TryParseVersion(existingRedirects[0].Attribute(Names.NewVersion)?.Value);
        }

        var parsedVersions = existingRedirects
            .Select(redirect => TryParseVersion(redirect.Attribute(Names.NewVersion)?.Value))
            .ToList();
        if (parsedVersions.Any(version => version == null))
        {
            logger.Warning($"Multiple redirections for {assemblyInfo.FullName.Name} found, but some newVersion values could not be parsed. Redirect update skipped.");
            return null;
        }

        var distinctVersions = parsedVersions
            .Cast<Version>()
            .Distinct()
            .ToList();
        if (distinctVersions.Count == 1)
        {
            logger.Debug($"Multiple redirections for {assemblyInfo.FullName.Name} resolved to the same newVersion {distinctVersions[0]}. They will be treated as one rule.");
            return distinctVersions[0];
        }

        return null;
    }

#pragma warning disable SA1202
    protected virtual Version? ReadFileVersion(string path)
    {
        try
        {
            return TryParseVersion(FileVersionInfo.GetVersionInfo(path).FileVersion);
        }
        catch (Exception)
        {
            return null;
        }
    }
#pragma warning restore SA1202

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

#pragma warning disable SA1201
    private enum CandidateSource
    {
        Profiler,
        CodeBase,
        Probe,
        ExistingRedirect,
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
#pragma warning restore SA1204
#pragma warning restore SA1201
}
#endif

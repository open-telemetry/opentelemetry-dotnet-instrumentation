// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CSharp;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Loader.Tests;

#pragma warning disable SA1118, SA1204
public sealed class AssemblyBindingUpdaterTests : IDisposable
{
    private const string TestAssemblyName = "TestRedirectedAssembly";
    private const string DefaultProfilerAssemblyVersion = "1.0.0.0";
    private const string DefaultProfilerFileVersion = "1.0.0.1";
    private const string ProfilerDirectoryName = "profiler";
    private static readonly XNamespace AsmNs = "urn:schemas-microsoft-com:asm.v1";
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "AssemblyBindingUpdaterTests", Guid.NewGuid().ToString("N"));

    public AssemblyBindingUpdaterTests()
    {
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesProfilerAssembly_WhenNoClientCandidateExists()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(updater, new AppDomainSetup { ApplicationBase = _rootDirectory });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(DefaultProfilerAssemblyVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(profilerAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_DoesNotProbeApplicationBase_WhenDisallowApplicationBaseProbingIsSet()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        _ = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", ".");
        _ = CreateAssembly(TestAssemblyName, "3.0.0.0", "3.0.0.1", "bin1");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                DisallowApplicationBaseProbing = true,
                PrivateBinPath = "bin1",
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(DefaultProfilerAssemblyVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(profilerAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(
            logger.DebugMessages,
            message => message.IndexOf("disallowApplicationBaseProbing=True", StringComparison.Ordinal) >= 0
                       && message.IndexOf("directories=[]", StringComparison.Ordinal) >= 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    public void ModifyAssemblyRedirectConfig_DoesNotProbeApplicationBase_WhenPrivateBinPathProbeIsNotNull(string privateBinPathProbe)
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        const string applicationBaseVersion = "4.0.0.0";
        const string expectedProbeVersion = "3.0.0.0";
        _ = CreateAssembly(TestAssemblyName, applicationBaseVersion, "4.0.0.1", ".");
        _ = CreateAssembly(TestAssemblyName, expectedProbeVersion, "3.0.0.1", "bin1");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);
        var expectedProbePath = Path.Combine(_rootDirectory, "bin1", $"{TestAssemblyName}.dll");

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                PrivateBinPath = "bin1",
                PrivateBinPathProbe = privateBinPathProbe,
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(expectedProbeVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(expectedProbePath).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(
            logger.DebugMessages,
            message => message.IndexOf("disableApplicationBaseProbe=True", StringComparison.Ordinal) >= 0
                       && message.IndexOf(Path.Combine(_rootDirectory, "bin1"), StringComparison.OrdinalIgnoreCase) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesRootedPrivateBinPath_WhenItIsUnderApplicationBase()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var rootedProbeAssembly = CreateAssembly(TestAssemblyName, "3.0.0.0", "3.0.0.1", "bin1");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                PrivateBinPath = Path.Combine(_rootDirectory, "bin1"),
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("3.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(rootedProbeAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_IgnoresRootedPrivateBinPath_WhenItIsOutsideApplicationBase()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var applicationBase = Path.Combine(_rootDirectory, "appbase");
        var outsideProbeDirectory = Path.Combine(_rootDirectory, "outside-bin");
        Directory.CreateDirectory(applicationBase);
        _ = CreateAssembly(TestAssemblyName, "3.0.0.0", "3.0.0.1", "outside-bin");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = applicationBase,
                PrivateBinPath = outsideProbeDirectory,
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(DefaultProfilerAssemblyVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(profilerAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(
            logger.WarningMessages,
            message => message.IndexOf("Ignoring rooted probing path", StringComparison.Ordinal) >= 0
                       && message.IndexOf(outsideProbeDirectory, StringComparison.OrdinalIgnoreCase) >= 0
                       && message.IndexOf(applicationBase, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesHigherCustomerCodeBase()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);
        const string customerVersion = "4.0.0.0";
        var customerHref = $"file:///customer/{TestAssemblyName}.dll";

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = _rootDirectory },
            CreateConfig(
                profilerAssembly,
                $$"""
                <dependentAssembly>
                  <assemblyIdentity name="{{TestAssemblyName}}" publicKeyToken="{TOKEN}" culture="neutral" />
                  <codeBase version="{{customerVersion}}" href="{{customerHref}}" />
                </dependentAssembly>
                """));
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(customerVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(customerHref, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesProbeAssembly_WhenAssemblyVersionMatchesAndFileVersionIsHigher()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", ProfilerDirectoryName);
        var probeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.5", "appbase");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(updater, new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(probeAssembly.Path)! });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("2.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(probeAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(logger.WarningMessages, message => message.IndexOf("File version comparison selected Probe", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesProfilerAssembly_WhenProbeFileVersionCannotBeRead()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.3", ProfilerDirectoryName);
        var probeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.5", "appbase");
        var logger = new TestLogger();
        var updater = CreateUpdater(
            logger,
            profilerAssembly,
            path => string.Equals(path, probeAssembly.Path, StringComparison.OrdinalIgnoreCase) ? null : ReadFileVersion(path));

        var result = ApplyConfig(updater, new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(probeAssembly.Path)! });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(new Uri(profilerAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_StopsAtFirstProbingHit()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var firstProbeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "bin1");
        _ = CreateAssembly(TestAssemblyName, "5.0.0.0", "5.0.0.1", "bin2");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                PrivateBinPath = "bin1;bin2",
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("2.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(firstProbeAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(logger.DebugMessages, message => message.IndexOf("Resolved probing directories", StringComparison.Ordinal) >= 0 && message.IndexOf("bin1", StringComparison.OrdinalIgnoreCase) >= 0 && message.IndexOf("bin2", StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.Contains(logger.DebugMessages, message => message.IndexOf("Discovered probing candidate", StringComparison.Ordinal) >= 0 && message.IndexOf(firstProbeAssembly.Path, StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.Equal(1, logger.DebugMessages.Count(message => message.IndexOf("Resolved probing directories", StringComparison.Ordinal) >= 0));
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesAssemblyNamedSubdirectoryWhenProbing()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var nestedProbeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", Path.Combine("bin1", TestAssemblyName));
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                PrivateBinPath = "bin1",
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("2.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(nestedProbeAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesAspNetBinFolderWhenConfigurationFileIsWebConfig()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var webBinAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "bin");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = _rootDirectory,
                ConfigurationFile = Path.Combine(_rootDirectory, "Web.config"),
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("2.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(webBinAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(
            logger.DebugMessages,
            message => message.IndexOf($"configurationFile='{Path.Combine(_rootDirectory, "Web.config")}'", StringComparison.OrdinalIgnoreCase) >= 0
                       && message.IndexOf(Path.Combine(_rootDirectory, "bin"), StringComparison.OrdinalIgnoreCase) >= 0
                       && message.IndexOf("isWebApplication", StringComparison.Ordinal) < 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_UsesCreatorApplicationBase_WhenApplicationBaseIsNotSet()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var probeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "bin1");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly, creatorApplicationBase: _rootDirectory);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                PrivateBinPath = "bin1",
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal("2.0.0.0", GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(probeAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_SkipsProbing_WhenApplicationBaseCannotBeResolved()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        _ = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "bin1");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly, creatorApplicationBase: string.Empty);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                PrivateBinPath = "bin1",
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(DefaultProfilerAssemblyVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(new Uri(profilerAssembly.Path).AbsoluteUri, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
        Assert.Contains(
            logger.WarningMessages,
            message => message.IndexOf("ApplicationBase could not be resolved", StringComparison.Ordinal) >= 0
                       && message.IndexOf("Probing for assembly binding update will be skipped", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_PreservesExistingHigherRedirectWithoutAddingCodeBase()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);
        const string preservedRedirectVersion = "5.0.0.0";
        var expectedOldVersion = $"0.0.0.0-{preservedRedirectVersion}";

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = _rootDirectory },
            CreateConfig(
                profilerAssembly,
                $$"""
                <dependentAssembly>
                  <assemblyIdentity name="{{TestAssemblyName}}" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="{{DefaultProfilerAssemblyVersion}}-{{preservedRedirectVersion}}" newVersion="{{preservedRedirectVersion}}" />
                </dependentAssembly>
                """));
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(expectedOldVersion, GetBindingRedirect(dependentAssembly).Attribute("oldVersion")?.Value);
        Assert.Null(dependentAssembly.Element(AsmNs + "codeBase"));
        Assert.Contains(logger.InformationMessages, message => message.IndexOf($"already targets higher version {preservedRedirectVersion}", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_ReplacesLowerRedirectWithHigherCandidate()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);
        const string lowerRedirectVersion = "2.0.0.0";
        const string customerVersion = "4.0.0.0";
        var customerHref = $"file:///customer/{TestAssemblyName}.dll";

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = _rootDirectory },
            CreateConfig(
                profilerAssembly,
                $$"""
                <dependentAssembly>
                  <assemblyIdentity name="{{TestAssemblyName}}" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="0.0.0.0-{{lowerRedirectVersion}}" newVersion="{{lowerRedirectVersion}}" />
                  <codeBase version="{{customerVersion}}" href="{{customerHref}}" />
                </dependentAssembly>
                """));
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);

        Assert.Equal(customerVersion, GetBindingRedirect(dependentAssembly).Attribute("newVersion")?.Value);
        Assert.Equal(customerHref, dependentAssembly.Element(AsmNs + "codeBase")?.Attribute("href")?.Value);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_DoesNotWriteBindingRedirects_WhenDisallowBindingRedirectsIsSet()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var probeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "appbase");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(probeAssembly.Path)!,
                DisallowBindingRedirects = true,
            });
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);
        var codeBaseVersions = dependentAssembly
            .Elements(AsmNs + "codeBase")
            .Select(codeBase => codeBase.Attribute("version")?.Value)
            .ToArray();

        Assert.Empty(dependentAssembly.Elements(AsmNs + "bindingRedirect"));
        Assert.Contains(DefaultProfilerAssemblyVersion, codeBaseVersions);
        Assert.Contains("2.0.0.0", codeBaseVersions);
        Assert.Contains(logger.WarningMessages, message => message.IndexOf("DisallowBindingRedirects", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_PreservesMultipleRedirectsAndAddsMissingCodeBases()
    {
        var profilerAssembly = CreateAssembly(TestAssemblyName, DefaultProfilerAssemblyVersion, DefaultProfilerFileVersion, ProfilerDirectoryName);
        var probeAssembly = CreateAssembly(TestAssemblyName, "2.0.0.0", "2.0.0.1", "appbase");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(probeAssembly.Path)! },
            CreateConfig(
                profilerAssembly,
                """
                <dependentAssembly>
                  <assemblyIdentity name="TestRedirectedAssembly" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="0.0.0.0-3.0.0.0" newVersion="3.0.0.0" />
                  <codeBase version="4.0.0.0" href="file:///customer/TestRedirectedAssembly.dll" />
                </dependentAssembly>
                <dependentAssembly>
                  <assemblyIdentity name="TestRedirectedAssembly" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
                </dependentAssembly>
                """));

        var dependentAssemblies = GetDependentAssemblies(result, profilerAssembly).ToList();
        var redirectCount = dependentAssemblies.SelectMany(element => element.Elements(AsmNs + "bindingRedirect")).Count();
        var codeBaseVersions = dependentAssemblies
            .SelectMany(element => element.Elements(AsmNs + "codeBase"))
            .Select(codeBase => codeBase.Attribute("version")?.Value)
            .ToArray();

        Assert.Equal(2, redirectCount);
        Assert.Contains("1.0.0.0", codeBaseVersions);
        Assert.Contains("2.0.0.0", codeBaseVersions);
        Assert.Contains("4.0.0.0", codeBaseVersions);
        Assert.Contains(logger.WarningMessages, message => message.IndexOf("Multiple redirections", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_TreatsMultipleRedirectsWithSameNewVersionAsOneRule()
    {
        var profilerAssembly = CreateAssembly("TestRedirectedAssembly", "1.0.0.0", "1.0.0.1", "profiler");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);
        const string rewrittenVersion = "5.0.0.0";
        var expectedOldVersion = $"0.0.0.0-{rewrittenVersion}";

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = _rootDirectory },
            CreateConfig(
                profilerAssembly,
                $$"""
                <dependentAssembly>
                  <assemblyIdentity name="TestRedirectedAssembly" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="1.0.0.0-{{rewrittenVersion}}" newVersion="{{rewrittenVersion}}" />
                </dependentAssembly>
                <dependentAssembly>
                  <assemblyIdentity name="TestRedirectedAssembly" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="3.0.0.0-{{rewrittenVersion}}" newVersion="{{rewrittenVersion}}" />
                </dependentAssembly>
                """));

        var dependentAssemblies = GetDependentAssemblies(result, profilerAssembly).ToList();
        var dependentAssembly = Assert.Single(dependentAssemblies);

        Assert.Equal(expectedOldVersion, GetBindingRedirect(dependentAssembly).Attribute("oldVersion")?.Value);
        Assert.Null(dependentAssembly.Element(AsmNs + "codeBase"));
        Assert.DoesNotContain(logger.WarningMessages, message => message.IndexOf("Multiple redirections", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ModifyAssemblyRedirectConfig_RemovesStaleCodeBasesDuringRewrite()
    {
        var profilerAssembly = CreateAssembly("TestRedirectedAssembly", "1.0.0.0", "1.0.0.1", "profiler");
        var logger = new TestLogger();
        var updater = CreateUpdater(logger, profilerAssembly);

        var result = ApplyConfig(
            updater,
            new AppDomainSetup { ApplicationBase = _rootDirectory },
            CreateConfig(
                profilerAssembly,
                """
                <dependentAssembly>
                  <assemblyIdentity name="TestRedirectedAssembly" publicKeyToken="{TOKEN}" culture="neutral" />
                  <bindingRedirect oldVersion="0.0.0.0-1.0.0.0" newVersion="1.0.0.0" />
                  <codeBase version="1.0.0.0" href="file:///stale/TestRedirectedAssembly.dll" />
                  <codeBase version="4.0.0.0" href="file:///customer/TestRedirectedAssembly.dll" />
                </dependentAssembly>
                """));
        var dependentAssembly = GetDependentAssembly(result, profilerAssembly);
        var codeBases = dependentAssembly.Elements(AsmNs + "codeBase").ToList();

        Assert.Single(codeBases);
        Assert.Equal("4.0.0.0", codeBases[0].Attribute("version")?.Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static AssemblyBindingUpdater CreateUpdater(
        IOtelLogger logger,
        AssemblyCatalog.AssemblyInfo profilerAssembly,
        Func<string, Version?>? fileVersionReader = null,
        string? creatorApplicationBase = null)
    {
        var catalog = new AssemblyCatalog([profilerAssembly]);
        return fileVersionReader == null && creatorApplicationBase == null
            ? new AssemblyBindingUpdater(logger, catalog)
            : new TestAssemblyBindingUpdater(logger, catalog, fileVersionReader, creatorApplicationBase);
    }

    private static XDocument ApplyConfig(AssemblyBindingUpdater updater, AppDomainSetup appDomainSetup, string? config = null)
    {
        if (!string.IsNullOrWhiteSpace(config))
        {
            appDomainSetup.SetConfigurationBytes(Encoding.UTF8.GetBytes(config));
        }

        updater.ModifyAssemblyRedirectConfig(appDomainSetup);

        var configBytes = Assert.IsType<byte[]>(appDomainSetup.GetConfigurationBytes());
        using var stream = new MemoryStream(configBytes);
        return XDocument.Load(stream);
    }

    private AssemblyCatalog.AssemblyInfo CreateAssembly(string assemblyName, string assemblyVersion, string fileVersion, string relativeDirectory)
    {
        var directory = Path.Combine(_rootDirectory, relativeDirectory);
        Directory.CreateDirectory(directory);
        var outputPath = Path.Combine(directory, $"{assemblyName}.dll");

        using var provider = new CSharpCodeProvider();
        var parameters = new CompilerParameters
        {
            GenerateExecutable = false,
            GenerateInMemory = false,
            OutputAssembly = outputPath,
            CompilerOptions = $"/target:library /keyfile:\"{FindKeyFile()}\"",
            IncludeDebugInformation = false,
            TreatWarningsAsErrors = false,
        };
        parameters.ReferencedAssemblies.Add("mscorlib.dll");

        var source = $$"""
            using System.Reflection;
            [assembly: AssemblyVersion("{{assemblyVersion}}")]
            [assembly: AssemblyFileVersion("{{fileVersion}}")]
            public sealed class Marker
            {
            }
            """;

        var results = provider.CompileAssemblyFromSource(parameters, source);
        if (results.Errors.HasErrors)
        {
            var message = string.Join(Environment.NewLine, results.Errors.Cast<CompilerError>().Select(error => error.ToString()));
            throw new InvalidOperationException(message);
        }

        var assemblyNameInfo = AssemblyName.GetAssemblyName(outputPath);
        var publicKeyToken = Assert.IsType<byte[]>(assemblyNameInfo.GetPublicKeyToken());
#pragma warning disable CA1308
        var token = BitConverter.ToString(publicKeyToken).ToLowerInvariant().Replace("-", string.Empty);
#pragma warning restore CA1308
        return new AssemblyCatalog.AssemblyInfo(token, assemblyNameInfo.Version, ReadFileVersion(outputPath), assemblyNameInfo, outputPath);
    }

    private static Version? ReadFileVersion(string path)
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
        return string.IsNullOrWhiteSpace(fileVersion) ? null : new Version(fileVersion);
    }

    private static string CreateConfig(AssemblyCatalog.AssemblyInfo assemblyInfo, string body)
        => $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <runtime>
                <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
                  {{body.Replace("{TOKEN}", assemblyInfo.Token)}}
                </assemblyBinding>
              </runtime>
            </configuration>
            """;

    private static XElement GetDependentAssembly(XDocument document, AssemblyCatalog.AssemblyInfo assemblyInfo)
        => Assert.Single(GetDependentAssemblies(document, assemblyInfo));

    private static IEnumerable<XElement> GetDependentAssemblies(XDocument document, AssemblyCatalog.AssemblyInfo assemblyInfo)
        => document
            .Descendants(AsmNs + "dependentAssembly")
            .Where(element =>
                string.Equals(element.Element(AsmNs + "assemblyIdentity")?.Attribute("name")?.Value, assemblyInfo.FullName.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(element.Element(AsmNs + "assemblyIdentity")?.Attribute("publicKeyToken")?.Value, assemblyInfo.Token, StringComparison.OrdinalIgnoreCase));

    private static XElement GetBindingRedirect(XElement dependentAssembly)
        => Assert.Single(dependentAssembly.Elements(AsmNs + "bindingRedirect"));

    private static string FindKeyFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "test-keypair.snk");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("test-keypair.snk not found.");
    }

    private sealed class TestLogger : IOtelLogger
    {
        public List<string> DebugMessages { get; } = [];

        public List<string> InformationMessages { get; } = [];

        public List<string> WarningMessages { get; } = [];

        public LogLevel Level => LogLevel.Debug;

        public bool IsEnabled(LogLevel level) => true;

        public void Debug(string messageTemplate, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T>(string messageTemplate, T property, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug(string messageTemplate, object[] args, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug(Exception exception, string messageTemplate, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Debug(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
        {
            DebugMessages.Add(messageTemplate);
        }

        public void Information(string messageTemplate, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T>(string messageTemplate, T property, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information(string messageTemplate, object[] args, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information(Exception exception, string messageTemplate, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Information(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
            => InformationMessages.Add(messageTemplate);

        public void Warning(string messageTemplate, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T>(string messageTemplate, T property, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning(string messageTemplate, object[] args, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning(Exception exception, string messageTemplate, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Warning(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
            => WarningMessages.Add(messageTemplate);

        public void Error(string messageTemplate, bool writeToEventLog = true)
        {
        }

        public void Error<T>(string messageTemplate, T property, bool writeToEventLog = true)
        {
        }

        public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
        {
        }

        public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
        {
        }

        public void Error(string messageTemplate, object[] args, bool writeToEventLog = true)
        {
        }

        public void Error(Exception exception, string messageTemplate, bool writeToEventLog = true)
        {
        }

        public void Error<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
        {
        }

        public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
        {
        }

        public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
        {
        }

        public void Error(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
        {
        }

        public void Close()
        {
        }
    }

    private sealed class TestAssemblyBindingUpdater(
        IOtelLogger logger,
        AssemblyCatalog assemblyCatalog,
        Func<string, Version?>? fileVersionReader,
        string? creatorApplicationBase)
        : AssemblyBindingUpdater(logger, assemblyCatalog)
    {
        protected override Version? ReadFileVersion(string path)
            => fileVersionReader == null ? base.ReadFileVersion(path) : fileVersionReader(path);

        protected override string? GetCreatorApplicationBase()
            => creatorApplicationBase ?? base.GetCreatorApplicationBase();
    }
}
#pragma warning restore SA1118, SA1204
#endif

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Xml.Linq;
using IntegrationTests.Helpers;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.BuildTasks.Tests;

public class TargetsPackageVersionTests
{
    [Fact]
    public void VerifyInstrumentationNuGetPackageVersions()
    {
        var solutionDirectory = EnvironmentTools.GetSolutionDirectory();
        var targetsFile = Path.Combine(solutionDirectory, "src", "OpenTelemetry.AutoInstrumentation.BuildTasks", "OpenTelemetry.AutoInstrumentation.BuildTasks.targets");

        var targetsDocument = XDocument.Load(targetsFile);
        var targetPackages = targetsDocument.Descendants("InstrumentationTarget")
            .Select(x => (Name: x.Attribute("InstrumentationNuGetPackageId")?.Value, Version: x.Attribute("InstrumentationNuGetPackageVersion")?.Value)).ToList();
        Assert.NotEmpty(targetPackages);

        var propsFile = Path.Combine(solutionDirectory, "src", "Directory.Packages.props");
        var propsDocument = XDocument.Load(propsFile);
        var propsDependencies = propsDocument.Descendants("PackageVersion")
            .Select(x => (Name: x.Attribute("Include")?.Value, Version: x.Attribute("Version")?.Value)).ToList();

        foreach (var targetPackage in targetPackages)
        {
            var propsDependency = Assert.Single(propsDependencies, d => d.Name == targetPackage.Name);
            Assert.Equal(propsDependency.Version, targetPackage.Version);
        }
    }
}

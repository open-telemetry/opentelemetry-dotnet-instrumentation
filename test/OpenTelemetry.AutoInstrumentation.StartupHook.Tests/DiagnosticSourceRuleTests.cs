// <copyright file="DiagnosticSourceRuleTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.AutoInstrumentation.RulesEngine;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.StartupHook.Tests;
public class DiagnosticSourceRuleTests
{
    [Theory]
    // These version are got when adding line `<PackageReference Include="System.Diagnostics.DiagnosticSource" VersionOverride="7.0.0" />` in Example.AspNetCoreMvc.csproj.

    // DS of major version lower than 7
    [InlineData("7.00.423.11508", true)]  // Version of DLL loaded when including version 4.7.0. It was auto-upgraded to 7.0.2, version of DS in store folder.

    // DS of major version of 7 but higher than auto instrumentation's version (7.0.0)
    [InlineData("7.00.22.51805", true)]   // Version of DLL loaded when including version 7.0.0.
    [InlineData("7.00.323.6910", true)]   // Version of DLL when including version 7.0.1.

    // DS of major version of 7 but lower than the auto-upgraded version (7.0.0)
    [InlineData("7.0.22.47203", false)]   // Version of DLL when including version 7.0.0-rc.2.22472.3 (had to use a pre-release version because 7.0.0 is the first official version of 7)

    // DS of major version higher than 7
    [InlineData("8.00.23.12803", true)]   // Version of DLL when including version 8.0.0-preview.2.23128.3
    public void DiagnosticSourceVersion(string diagnosticSourceVersion, bool result)
    {
        var rule = new TestableDiagnosticSourceRule(diagnosticSourceVersion);
        Assert.Equal(rule.Evaluate(), result);
    }
}

internal class TestableDiagnosticSourceRule : DiagnosticSourceRule
{
    private readonly string diagnosticSourceVersion;

    public TestableDiagnosticSourceRule(string diagnosticSourceVersion)
    {
        this.diagnosticSourceVersion = diagnosticSourceVersion;
    }

    protected override string? GetDiagnosticSourceVersion()
    {
        return diagnosticSourceVersion;
    }
}

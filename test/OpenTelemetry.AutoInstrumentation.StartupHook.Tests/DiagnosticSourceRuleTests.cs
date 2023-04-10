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
    [InlineData("6.0.0.0", "7.0.0.0", false)]
    [InlineData("8.0.0.0", "7.0.0.0", true)]
    [InlineData("7.0.0.0", "7.0.0.0", true)]
    public void DiagnosticSourceVersion(string appVersion, string autoInstrumentationVersion, bool result)
    {
        var rule = new TestableDiagnosticSourceRule(appVersion, autoInstrumentationVersion);
        Assert.Equal(rule.Evaluate(), result);
    }
}

internal class TestableDiagnosticSourceRule : DiagnosticSourceRule
{
    private readonly string appVersion;
    private readonly string autoInstrumentationVersion;

    public TestableDiagnosticSourceRule(string appVersion, string autoInstrumentationVersion)
    {
        this.appVersion = appVersion;
        this.autoInstrumentationVersion = autoInstrumentationVersion;
    }

    protected override Version? GetVersionFromApp()
    {
        return new Version(appVersion);
    }

    protected override Version? GetVersionFromAutoInstrumentation()
    {
        return new Version(autoInstrumentationVersion);
    }
}

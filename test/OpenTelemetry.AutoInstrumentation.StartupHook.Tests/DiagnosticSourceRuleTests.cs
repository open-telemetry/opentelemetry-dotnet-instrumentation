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

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.StartupHook.Tests;
public class DiagnosticSourceRuleTests
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public static IEnumerable<object[]> RuleTestData =>
       new List<object[]>
       {
                new object[] { "6.0.0.0", "7.0.0.0", "Rule Engine: Application has direct or indirect reference to older version of System.Diagnostics.DiagnosticSource.dll 6.0.0.0.", false },
                new object[] { "8.0.0.0", "7.0.0.0", "Rule Engine: DiagnosticSourceRule evaluation success.", true },
                new object[] { "7.0.0.0", "7.0.0.0", "Rule Engine: DiagnosticSourceRule evaluation success.", true },
                new object[] { null, "7.0.0.0", "Rule Engine: DiagnosticSourceRule evaluation success.", true },
       };
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [Theory]
    [MemberData(nameof(RuleTestData))]
    public void DiagnosticSourceVersion(string appVersion, string autoInstrumentationVersion, string logMessage, bool result)
    {
        var logger = new TestLogger();
        var rule = new TestableDiagnosticSourceRule(appVersion, autoInstrumentationVersion, logger);
        Assert.Equal(result, rule.Evaluate());
        Assert.Equal(logMessage, logger.LogRecords[0].Message);
    }
}

internal class TestableDiagnosticSourceRule : DiagnosticSourceRule
{
    private readonly string appVersion;
    private readonly string autoInstrumentationVersion;

    public TestableDiagnosticSourceRule(string appVersion, string autoInstrumentationVersion, IOtelLogger otelLogger)
        : base(otelLogger)
    {
        this.appVersion = appVersion;
        this.autoInstrumentationVersion = autoInstrumentationVersion;
    }

    protected override Version? GetResolvedVersion()
    {
        if (appVersion == null)
        {
            return null;
        }

        return new Version(appVersion);
    }

    protected override Version GetVersionFromAutoInstrumentation()
    {
        return new Version(autoInstrumentationVersion);
    }
}

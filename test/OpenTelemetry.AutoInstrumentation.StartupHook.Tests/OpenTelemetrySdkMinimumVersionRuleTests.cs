// <copyright file="OpenTelemetrySdkMinimumVersionRuleTests.cs" company="OpenTelemetry Authors">
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

public class OpenTelemetrySdkMinimumVersionRuleTests
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public static IEnumerable<object[]> RuleTestData =>
       new List<object[]>
       {
                new object[] { new Version("1.0.0.0"), new Version("2.0.0.0"), "Rule Engine: Application has direct or indirect reference to older version of OpenTelemetry package 1.0.0.0.", false },
                new object[] { new Version("2.0.0.0"), new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
                new object[] { new Version("1.0.0.0"), new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
                new object[] { null, new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
                new object[] { new Version("1.0.0.0"), null, "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
                new object[] { null, null, "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
       };
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [Theory]
    [MemberData(nameof(RuleTestData))]
    public void OpenTelemetrySdkMinimumVersionRuleValidation(Version appVersion, Version autoInstrumentationVersion, string logMessage, bool result)
    {
        var logger = new TestLogger();
        var rule = new OpenTelemetrySdkMinimumVersionTestRule(appVersion, autoInstrumentationVersion, logger);
        var ruleResult = rule.Evaluate();
        Assert.Equal(logMessage, logger.LogRecords[0].Message);
        Assert.Equal(result, ruleResult);
    }

    private class OpenTelemetrySdkMinimumVersionTestRule : OpenTelemetrySdkMinimumVersionRule
    {
        private readonly Version _appVersion;
        private readonly Version _autoInstrumentationVersion;

        public OpenTelemetrySdkMinimumVersionTestRule(Version appVersion, Version autoInstrumentationVersion, IOtelLogger otelLogger)
            : base(otelLogger)
        {
            _appVersion = appVersion;
            _autoInstrumentationVersion = autoInstrumentationVersion;
        }

        protected override Version? GetVersionFromApp()
        {
            return _appVersion;
        }

        protected override Version? GetVersionFromAutoInstrumentation()
        {
            return _autoInstrumentationVersion;
        }
    }
}

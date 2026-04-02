// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.StartupHook.Tests;

public class OpenTelemetrySdkMinimumVersionRuleTests
{
    public static TheoryData<Version?, Version?, string, bool> RuleTestData()
    {
        return new TheoryData<Version?, Version?, string, bool>
        {
            { new Version("1.0.0.0"), new Version("2.0.0.0"), "Rule Engine: Application has direct or indirect reference to older version of OpenTelemetry package 1.0.0.0.", false },
            { new Version("2.0.0.0"), new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
            { new Version("1.0.0.0"), new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
            { null, new Version("1.0.0.0"), "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
            { new Version("1.0.0.0"), null, "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true },
            { null, null, "Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.", true }
        };
    }

    [Theory]
    [MemberData(nameof(RuleTestData))]
    public void OpenTelemetrySdkMinimumVersionRuleValidation(Version? appVersion, Version? autoInstrumentationVersion, string logMessage, bool result)
    {
        var logger = new TestLogger();
        var rule = new OpenTelemetrySdkMinimumVersionTestRule(appVersion, autoInstrumentationVersion, logger);
        var ruleResult = rule.Evaluate();
        Assert.Equal(logMessage, logger.LogRecords[0].Message);
        Assert.Equal(result, ruleResult);
    }

    private sealed class OpenTelemetrySdkMinimumVersionTestRule : OpenTelemetrySdkMinimumVersionRule
    {
        private readonly Version? _appVersion;
        private readonly Version? _autoInstrumentationVersion;

        public OpenTelemetrySdkMinimumVersionTestRule(Version? appVersion, Version? autoInstrumentationVersion, IOtelLogger otelLogger)
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

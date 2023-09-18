// <copyright file="RuleEngineTests.cs" company="OpenTelemetry Authors">
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

public class RuleEngineTests : IDisposable
{
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", null);
    }

    [Fact]
    public void RuleEngineValidation_WhenShouldTrackIsTrue()
    {
        // Arrange
        SetShouldTrackEnvironmentVariable(true);
        var testRule = new TestRule();
        var ruleEngine = new RuleEngine(new Lazy<List<Rule>>(() => new() { testRule }));

        // Act
        var result = ruleEngine.ValidateRules();

        // Assert
        Assert.True(result);
        Assert.True(testRule.IsEvaluated);
    }

    [Fact]
    public void RuleEngineValidation_WhenShouldTrackIsFalse()
    {
        // Arrange
        SetShouldTrackEnvironmentVariable(false);
        var testRule = new TestRule();
        var ruleEngine = new RuleEngine(new Lazy<List<Rule>>(() => new() { testRule }));

        // Act
        var result = ruleEngine.ValidateRules();

        // Assert
        Assert.True(result);
        Assert.False(testRule.IsEvaluated);
    }

    [Fact]
    public void RuleEngineValidation_WhenShouldTrackIsNull()
    {
        // Arrange
        SetShouldTrackEnvironmentVariable(null);
        var testRule = new TestRule();
        var ruleEngine = new RuleEngine(new Lazy<List<Rule>>(() => new() { testRule }));

        // Act
        var result = ruleEngine.ValidateRules();

        // Assert
        Assert.True(result);
        Assert.True(testRule.IsEvaluated);
    }

    [Fact]
    public void RuleEngineValidation_WhenShouldTrackIsNotSet()
    {
        // Arrange
        var testRule = new TestRule();
        var ruleEngine = new RuleEngine(new Lazy<List<Rule>>(() => new() { testRule }));

        // Act
        var result = ruleEngine.ValidateRules();

        // Assert
        Assert.True(result);
        Assert.True(testRule.IsEvaluated);
    }

    [Fact]
    public void RuleEngineValidation_WhenShouldTrackHasInvalidValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", "Invalid");
        var testRule = new TestRule();
        var ruleEngine = new RuleEngine(new Lazy<List<Rule>>(() => new() { testRule }));

        // Act
        var result = ruleEngine.ValidateRules();

        // Assert
        Assert.True(result);
        Assert.True(testRule.IsEvaluated);
    }

    private void SetShouldTrackEnvironmentVariable(bool? value)
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", value?.ToString());
    }

    private class TestRule : Rule
    {
        internal bool IsEvaluated { get; private set; }

        internal override bool Evaluate()
        {
            IsEvaluated = true;
            return true;
        }
    }
}

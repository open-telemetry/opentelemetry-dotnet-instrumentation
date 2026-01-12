// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.RulesEngine;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.StartupHook.Tests;

public sealed class RuleEngineTests : IDisposable
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

    private static void SetShouldTrackEnvironmentVariable(bool? value)
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

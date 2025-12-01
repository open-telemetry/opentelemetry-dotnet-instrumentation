// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class RuleEngine
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    private readonly List<Rule> _mandatoryRules = new()
    {
        new ApplicationInExcludeListRule(),
        new MinSupportedFrameworkRule(),
        new EndOfSupportRule()
    };

    private readonly Lazy<List<Rule>> _optionalRules;

    internal RuleEngine()
        : this(new Lazy<List<Rule>>(CreateDefaultOptionalRules))
    {
    }

    // This constructor is used for test purpose.
    internal RuleEngine(Lazy<List<Rule>> optionalRules)
    {
        _optionalRules = optionalRules;
    }

    internal bool ValidateRules()
    {
        var result = true;

        // Single rule failure will stop the execution.
        foreach (var rule in _mandatoryRules)
        {
            if (!EvaluateRule(rule))
            {
                return false;
            }
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED"), out var shouldTrack) && !shouldTrack)
        {
            Logger.Information($"OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED is set to false, skipping rule engine validation.");
            return result;
        }

        // All the rules are validated here.
        foreach (var rule in _optionalRules.Value)
        {
            if (!EvaluateRule(rule))
            {
                result = false;
            }
        }

        return result;
    }

    private static bool EvaluateRule(Rule rule)
    {
        try
        {
            if (!rule.Evaluate())
            {
                Logger.Error($"Rule '{rule.Name}' failed: {rule.Description}");
                return false;
            }
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception
        {
            Logger.Warning($"Error evaluating rule '{rule.Name}': {ex.Message}");
        }

        return true;
    }

    private static List<Rule> CreateDefaultOptionalRules()
    {
        return new()
        {
            new RuntimeStoreDiagnosticRule(),
            new OpenTelemetrySdkMinimumVersionRule(),
            new AssemblyFileVersionRule(),
            new NativeProfilerDiagnosticsRule()
        };
    }
}

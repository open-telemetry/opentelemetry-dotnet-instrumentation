// <copyright file="RuleEngine.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class RuleEngine
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    private readonly List<Rule> _mandatoryRules = new()
    {
        new ApplicationInExcludeListRule(),
        new MinSupportedFrameworkRule()
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
        catch (Exception ex)
        {
            Logger.Warning($"Error evaluating rule '{rule.Name}': {ex.Message}");
        }

        return true;
    }

    private static List<Rule> CreateDefaultOptionalRules()
    {
        return new()
        {
            new OpenTelemetrySdkMinimumVersionRule(),
            new AssemblyFileVersionRule(),
            new NativeProfilerDiagnosticsRule()
        };
    }
}

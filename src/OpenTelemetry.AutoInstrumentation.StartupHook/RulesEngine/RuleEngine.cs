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

    private readonly List<Rule> _rules = new()
    {
        new OpenTelemetrySdkMinimumVersionRule(),
        new DiagnosticSourceRule(),
        new InstrumentationAssemblyRule()
    };

    internal bool Validate()
    {
        var result = true;

        foreach (var rule in _rules)
        {
            try
            {
                if (!rule.Evaluate())
                {
                    Logger.Error($"Rule '{rule.Name}' failed: {rule.Description}");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error evaluating rule '{rule.Name}': {ex.Message}");
                result = false;
            }
        }

        return result;
    }
}

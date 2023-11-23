// <copyright file="MinSupportedFrameworkRule.cs" company="OpenTelemetry Authors">
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

internal class MinSupportedFrameworkRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public MinSupportedFrameworkRule()
    {
        Name = "Minimum Supported Framework Version Validator";
        Description = "Verifies that the application is running on a supported version of the .NET runtime.";
    }

    internal override bool Evaluate()
    {
        Version minRequiredFrameworkVersion = new(6, 0);
        var frameworkVersion = Environment.Version;
        if (frameworkVersion < minRequiredFrameworkVersion)
        {
            Logger.Information($"Rule Engine: Error in StartupHook initialization: {frameworkVersion} is not supported");
            return false;
        }

        Logger.Information("Rule Engine: MinSupportedFrameworkRule evaluation success.");
        return true;
    }
}

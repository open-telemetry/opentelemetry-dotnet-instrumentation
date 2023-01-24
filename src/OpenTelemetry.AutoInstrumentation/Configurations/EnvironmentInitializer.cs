// <copyright file="EnvironmentInitializer.cs" company="OpenTelemetry Authors">
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

using System.Collections.Specialized;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// EnvironmentSetter is initializing the OTEL_* environemtal variables
/// with provided values if they are not already set.
/// </summary>
internal class EnvironmentInitializer
{
    private const string VariablePrefix = "OTEL_";

    public static void Initialize(NameValueCollection nameValueCollection)
    {
        foreach (var setting in nameValueCollection.AllKeys)
        {
            if (setting == null)
            {
                continue;
            }

            if (!setting.StartsWith(VariablePrefix))
            {
                // not OTEL_ setting
                continue;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(setting)))
            {
                // already set via env var - to not override
                continue;
            }

            Environment.SetEnvironmentVariable(setting, nameValueCollection[setting]);
        }
    }
}

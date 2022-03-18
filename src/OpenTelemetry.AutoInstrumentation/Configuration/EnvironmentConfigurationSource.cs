// <copyright file="EnvironmentConfigurationSource.cs" company="OpenTelemetry Authors">
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

using System;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Represents a configuration source that
/// retrieves values from environment variables.
/// </summary>
public class EnvironmentConfigurationSource : StringConfigurationSource
{
    /// <inheritdoc />
    public override string GetString(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch
        {
            // We should not add a dependency from the Configuration system to the Logger system,
            // so do nothing
        }

        return null;
    }
}

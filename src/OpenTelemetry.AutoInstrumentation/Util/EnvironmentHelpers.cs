// <copyright file="EnvironmentHelpers.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Helpers to access environment variables
/// </summary>
internal static class EnvironmentHelpers
{
    private static readonly ILogger Logger = ConsoleLogger.Create(typeof(EnvironmentHelpers));

    /// <summary>
    /// Safe wrapper around Environment.GetEnvironmentVariable
    /// </summary>
    /// <param name="key">Name of the environment variable to fetch</param>
    /// <param name="defaultValue">Value to return in case of error</param>
    /// <returns>The value of the environment variable, or the default value if an error occured</returns>
    public static string GetEnvironmentVariable(string key, string defaultValue = null)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error while reading environment variable {EnvironmentVariable}", key);
        }

        return defaultValue;
    }
}

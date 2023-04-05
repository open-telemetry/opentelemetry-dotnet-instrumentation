// <copyright file="RuleEngineTracker.cs" company="OpenTelemetry Authors">
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

using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class RuleEngineTracker
{
    internal const string FileNamePrefix = "rule_engine_tracker_";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");
    private readonly bool? _shouldTrack;

    public RuleEngineTracker()
    {
        var ruleEngineEnabled = Environment.GetEnvironmentVariable("OTEL_RULE_ENGINE_ENABLED");
        if (ruleEngineEnabled == null)
        {
            _shouldTrack = null;
        }
        else
        {
            bool.TryParse(ruleEngineEnabled, out var shouldTrack);
            _shouldTrack = shouldTrack;
        }
    }

    public void CreateFile()
    {
        if (_shouldTrack != false)
        {
            var filePath = GetFilePath();
            try
            {
                File.Create(filePath).Dispose();
            }
            catch (Exception ex)
            {
                // Not a critical exception, if file is not created
                // validation happens everytime.
                Logger.Warning($"Failed to create RuleEngine file: {ex.Message}");
            }
        }
    }

    public void DeleteTrackerFile()
    {
        var filePath = GetFilePath();
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            // Not a critical exception, when file exists rule engine does not validate.
            Logger.Warning($"Failed to delete RuleEngine file: {ex.Message}");
        }
    }

    public bool IsFileExists()
    {
        var filePath = GetFilePath();

        if (_shouldTrack == true)
        {
            DeleteTrackerFile();
        }

        return _shouldTrack == true || File.Exists(filePath);
    }

    private string GetFilePath()
    {
        var processName = Process.GetCurrentProcess().ProcessName;
        var path = AppContext.BaseDirectory;
        var username = Environment.UserName;
        var dataToHash = $"{processName}-{path}-{username}";

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        var hashString = Convert.ToBase64String(hash)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", string.Empty);

        var tempDirPath = Path.GetTempPath();
        return Path.Combine(tempDirPath, $"{FileNamePrefix}{hashString}.tmp");
    }
}

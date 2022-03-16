// <copyright file="DomainMetadata.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Dedicated helper class for consistently referencing Process and AppDomain information.
/// </summary>
internal static class DomainMetadata
{
    private const string UnknownName = "unknown";
    private static bool _initialized;
    private static string _currentProcessName;
    private static string _currentProcessMachineName;
    private static int _currentProcessId;
    private static bool _processDataUnavailable;
    private static bool _domainDataPoisoned;
    private static bool? _isAppInsightsAppDomain;

    static DomainMetadata()
    {
        TrySetProcess();
    }

    public static string ProcessName
    {
        get
        {
            return !_processDataUnavailable ? _currentProcessName : UnknownName;
        }
    }

    public static string MachineName
    {
        get
        {
            return !_processDataUnavailable ? _currentProcessMachineName : UnknownName;
        }
    }

    public static int ProcessId
    {
        get
        {
            return !_processDataUnavailable ? _currentProcessId : -1;
        }
    }

    public static string AppDomainName
    {
        get
        {
            try
            {
                return !_domainDataPoisoned ? AppDomain.CurrentDomain.FriendlyName : UnknownName;
            }
            catch
            {
                _domainDataPoisoned = true;
                return UnknownName;
            }
        }
    }

    public static int AppDomainId
    {
        get
        {
            try
            {
                return !_domainDataPoisoned ? AppDomain.CurrentDomain.Id : -1;
            }
            catch
            {
                _domainDataPoisoned = true;
                return -1;
            }
        }
    }

    public static bool ShouldAvoidAppDomain()
    {
        if (_isAppInsightsAppDomain == null)
        {
            _isAppInsightsAppDomain = AppDomainName.IndexOf("ApplicationInsights", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return _isAppInsightsAppDomain.Value;
    }

    private static void TrySetProcess()
    {
        try
        {
            if (!_processDataUnavailable && !_initialized)
            {
                _initialized = true;
                ProcessHelpers.GetCurrentProcessInformation(out _currentProcessName, out _currentProcessMachineName, out _currentProcessId);
            }
        }
        catch
        {
            _processDataUnavailable = true;
        }
    }
}

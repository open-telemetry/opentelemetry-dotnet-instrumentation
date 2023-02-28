// <copyright file="NoopLogger.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Do-nothing implementation used when no-logging is requested.
/// </summary>
internal class NoopLogger : IOtelLogger
{
    public static readonly NoopLogger Instance = new();

    private NoopLogger()
    {
    }

    public bool IsEnabled(LogLevel level)
    {
        return false;
    }

    public void Debug(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Debug<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Debug(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Debug(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Debug<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Debug(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Information(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Information<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Information(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Information(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Information<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Information(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Warning(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Warning<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Warning(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Warning(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Warning<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Warning(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Error(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Error<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Error(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Error(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Error<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Error(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal interface IOtelLogger : IDisposable
{
    LogLevel Level { get; }

    bool IsEnabled(LogLevel level);

    void Debug(string messageTemplate, bool writeToEventLog = true);

    void Debug<T>(string messageTemplate, T property, bool writeToEventLog = true);

    void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Debug(string messageTemplate, object[] args, bool writeToEventLog = true);

    void Debug(Exception exception, string messageTemplate, bool writeToEventLog = true);

    void Debug<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true);

    void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Debug(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true);

    void Information(string messageTemplate, bool writeToEventLog = true);

    void Information<T>(string messageTemplate, T property, bool writeToEventLog = true);

    void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Information(string messageTemplate, object[] args, bool writeToEventLog = true);

    void Information(Exception exception, string messageTemplate, bool writeToEventLog = true);

    void Information<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true);

    void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Information(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true);

    void Warning(string messageTemplate, bool writeToEventLog = true);

    void Warning<T>(string messageTemplate, T property, bool writeToEventLog = true);

    void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Warning(string messageTemplate, object[] args, bool writeToEventLog = true);

    void Warning(Exception exception, string messageTemplate, bool writeToEventLog = true);

    void Warning<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true);

    void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Warning(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true);

    void Error(string messageTemplate, bool writeToEventLog = true);

    void Error<T>(string messageTemplate, T property, bool writeToEventLog = true);

    void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Error(string messageTemplate, object[] args, bool writeToEventLog = true);

    void Error(Exception exception, string messageTemplate, bool writeToEventLog = true);

    void Error<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true);

    void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true);

    void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true);

    void Error(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true);
}

// <copyright file="IOtelLogger.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Logging;

#pragma warning disable CS1591
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Not")]
public interface IOtelLogger
{
    bool IsEnabled(LogLevel level);

    void Debug(string messageTemplate);

    void Debug<T>(string messageTemplate, T property);

    void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1);

    void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Debug(string messageTemplate, object[] args);

    void Debug(Exception exception, string messageTemplate);

    void Debug<T>(Exception exception, string messageTemplate, T property);

    void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1);

    void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Debug(Exception exception, string messageTemplate, object[] args);

    void Information(string messageTemplate);

    void Information<T>(string messageTemplate, T property);

    void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1);

    void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Information(string messageTemplate, object[] args);

    void Information(Exception exception, string messageTemplate);

    void Information<T>(Exception exception, string messageTemplate, T property);

    void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1);

    void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Information(Exception exception, string messageTemplate, object[] args);

    void Warning(string messageTemplate);

    void Warning<T>(string messageTemplate, T property);

    void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1);

    void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Warning(string messageTemplate, object[] args);

    void Warning(Exception exception, string messageTemplate);

    void Warning<T>(Exception exception, string messageTemplate, T property);

    void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1);

    void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Warning(Exception exception, string messageTemplate, object[] args);

    void Error(string messageTemplate);

    void Error<T>(string messageTemplate, T property);

    void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1);

    void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Error(string messageTemplate, object[] args);

    void Error(Exception exception, string messageTemplate);

    void Error<T>(Exception exception, string messageTemplate, T property);

    void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1);

    void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2);

    void Error(Exception exception, string messageTemplate, object[] args);
}
#pragma warning restore CS1591

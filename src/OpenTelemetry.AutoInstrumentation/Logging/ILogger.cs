// <copyright file="ILogger.cs" company="OpenTelemetry Authors">
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
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal interface ILogger
{
    bool IsEnabled(LogLevel level);

    void Debug(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Debug(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Information(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Warning(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");

    void Error(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "");
}

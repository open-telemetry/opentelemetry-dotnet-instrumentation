// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process.DuckTypes;

internal interface IProcessStartInfo
{
    bool UseShellExecute { get; }

    IDictionary<string, string?> Environment { get; }

    StringDictionary EnvironmentVariables { get; }

#if NET
    [DuckField(Name = "_environmentVariables")]
#else
    [DuckField(Name = "environmentVariables")]
#endif
    object? EnvironmentVariablesField { get; set; }

#if NETFRAMEWORK
    [DuckField(Name = "environment")]
    object? EnvironmentField { get; set; }
#endif

    [Duck(Name = "MemberwiseClone")]
    object Clone();
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Process.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process;

internal static class ProcessStartInfoCloner
{
    internal static TStartInfo Prepare<TStartInfo>(TStartInfo startInfo, Action<IDictionary<string, string?>> inject)
    {
        var original = startInfo.DuckCast<IProcessStartInfo>()!;
        if (original.UseShellExecute)
        {
            return startInfo;
        }

        var clone = original.Clone();
        var clonedStartInfo = clone.DuckCast<IProcessStartInfo>();

        // Detach the clone's environment without reading the original's lazy getter.
        // Reading that getter would freeze future environment inheritance on Windows.
        clonedStartInfo.EnvironmentVariablesField = null;
#if NETFRAMEWORK
        clonedStartInfo.EnvironmentField = null;
#endif

        var environment = clonedStartInfo.Environment;
        if (original.EnvironmentVariablesField is not null)
        {
            environment.Clear();
            foreach (DictionaryEntry entry in original.EnvironmentVariables)
            {
                environment.Add((string)entry.Key, (string?)entry.Value);
            }
        }

        inject(environment);
        return (TStartInfo)clone;
    }
}

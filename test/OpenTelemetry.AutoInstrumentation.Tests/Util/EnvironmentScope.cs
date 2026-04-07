// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

internal sealed class EnvironmentScope : IDisposable
{
#if NET9_0_OR_GREATER
    private static readonly Lock GlobalLock = new();
#else
    private static readonly object GlobalLock = new();
#endif
    private static bool isGlobalScopeAquired;
    private readonly Dictionary<string, string?> originalValues = [];

    public EnvironmentScope(Dictionary<string, string?> variables)
    {
        lock (GlobalLock)
        {
            if (isGlobalScopeAquired)
            {
                throw new InvalidOperationException(
                    "Another test is currently modifying environment variables. Parallel environment manipulation is not allowed.");
            }

            isGlobalScopeAquired = true;
        }

        try
        {
            foreach (var pair in variables)
            {
                originalValues[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
        catch
        {
            ReleaseGlobalLock();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            foreach (var pair in originalValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
        finally
        {
            ReleaseGlobalLock();
        }
    }

    private static void ReleaseGlobalLock()
    {
        lock (GlobalLock)
        {
            isGlobalScopeAquired = false;
        }
    }
}

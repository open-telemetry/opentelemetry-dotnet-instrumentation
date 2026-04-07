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
    private static bool isGlobalScopeAсquired;
    private readonly Dictionary<string, string?> originalValues = [];

    private int disposedState;

    public EnvironmentScope(IReadOnlyDictionary<string, string?> variables)
    {
        lock (GlobalLock)
        {
            if (isGlobalScopeAсquired)
            {
                throw new InvalidOperationException(
                    "Another test is currently modifying environment variables. Parallel environment manipulation is not allowed.");
            }

            isGlobalScopeAсquired = true;
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
        if (Interlocked.Exchange(ref disposedState, 1) == 1)
        {
            return;
        }

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
            isGlobalScopeAсquired = false;
        }
    }
}

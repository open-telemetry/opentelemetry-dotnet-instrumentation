// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal class InstrumentationLifespanManager : ILifespanManager
{
    // some instrumentations requires to keep references to objects
    // so that they are not garbage collected
    private readonly ConcurrentBag<object> _instrumentations = [];

    public void Track(object instance)
    {
        _instrumentations.Add(instance);
    }

    public void Dispose()
    {
        while (_instrumentations.TryTake(out var instrumentation))
        {
            if (instrumentation is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

internal abstract class Processor : IProcessor
{
    protected Processor()
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual Task FlushAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public virtual Task ShutdownAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected virtual void Dispose(bool disposing)
    {
    }
}

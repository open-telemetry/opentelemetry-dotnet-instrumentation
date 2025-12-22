// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ProfilerSpanStoppageHandling;

// A dependency that queues work in it's ctor.
public sealed class TestDependency : IDisposable
{
    private readonly Task _task;
    private readonly ManualResetEventSlim _resetEvent = new(false);

    public TestDependency()
    {
        _task = Task.Run(Loop);
    }

    public void Dispose()
    {
        _resetEvent.Set();
        _task.Wait(TimeSpan.FromMilliseconds(500));
        _resetEvent.Dispose();
    }

    private void Loop()
    {
        _resetEvent.Wait();
    }
}

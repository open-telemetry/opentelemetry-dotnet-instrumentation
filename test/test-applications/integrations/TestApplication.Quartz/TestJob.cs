// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Quartz;

namespace TestApplication.Quartz;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Quartz.NET.
internal sealed class TestJob : IJob
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Quartz.NET.
{
#if QUARTZ_4_0_0_OR_GREATER
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
#else
    public Task Execute(IJobExecutionContext context)
#endif
    {
        Console.WriteLine($"Job: '{nameof(TestJob)}' executed");
#if QUARTZ_4_0_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return Task.CompletedTask;
#endif
    }
}

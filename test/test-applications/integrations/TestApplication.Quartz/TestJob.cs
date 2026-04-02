// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Quartz;

namespace TestApplication.Quartz;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Quartz.NET.
internal sealed class TestJob : IJob
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Quartz.NET.
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"Job: '{nameof(TestJob)}' executed");
        return Task.CompletedTask;
    }
}

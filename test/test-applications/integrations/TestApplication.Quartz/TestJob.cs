// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Quartz;

namespace TestApplication.Quartz;

public class TestJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"Job: '{nameof(TestJob)}' executed");
        return Task.CompletedTask;
    }
}

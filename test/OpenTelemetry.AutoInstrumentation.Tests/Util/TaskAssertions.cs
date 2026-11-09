// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

internal static class TaskAssertions
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    public static async Task CompletesAsync(Task task)
    {
        using var timeoutCancellation = new CancellationTokenSource();

        try
        {
            var completedTask = await Task.WhenAny(
                task,
                Task.Delay(TestTimeout, timeoutCancellation.Token))
                .ConfigureAwait(false);

            Assert.Same(task, completedTask);
            await task.ConfigureAwait(false);
        }
        finally
        {
#if NET
            await timeoutCancellation.CancelAsync().ConfigureAwait(false);
#else
            timeoutCancellation.Cancel();
#endif
        }
    }
}

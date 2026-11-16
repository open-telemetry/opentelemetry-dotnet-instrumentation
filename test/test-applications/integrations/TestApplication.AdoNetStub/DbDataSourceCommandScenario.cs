// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Data.Common;
using System.Diagnostics;

namespace TestApplication.AdoNetStub;

internal static class DbDataSourceCommandScenario
{
    public static async Task RunAsync(bool completeSynchronously)
    {
        using var source = new ActivitySource("TestApplication.AdoNetStub");

        await ExecuteCommandAsync("ExecuteScalarAsync", command => command.ExecuteScalarAsync()).ConfigureAwait(false);
        await ExecuteCommandAsync("ExecuteNonQueryAsync", command => command.ExecuteNonQueryAsync()).ConfigureAwait(false);
        await ExecuteCommandAsync("ExecuteReaderAsync", command => command.ExecuteReaderAsync()).ConfigureAwait(false);

        async Task ExecuteCommandAsync<T>(string operation, Func<DbCommand, Task<T>> execute)
        {
            using var parent = source.StartActivity($"{operation}.parent")
                               ?? throw new InvalidOperationException("The parent activity was not created.");
            var gate = completeSynchronously ? null : new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var dataSource = new StubDbProviderFactory(gate?.Task).CreateDataSource(string.Empty);
            using var command = dataSource.CreateCommand("SELECT * FROM FakeTable");

            // The default provider factory returns the framework's command wrapper, whose Connection getter throws.
            // Keep the execute delegate synchronous so an async helper cannot hide changes to the caller's context.
#pragma warning disable CA2025 // The delegate returns the command task, which is awaited below before disposal.
            var task = execute(command);
#pragma warning restore CA2025
            var activityAfterReturn = Activity.Current;
            var completedOnReturn = task.IsCompleted;
            gate?.SetResult(true);
            var result = await task.ConfigureAwait(false);
            using var disposableResult = result as IDisposable;

            if (!ReferenceEquals(parent, activityAfterReturn))
            {
                throw new InvalidOperationException($"{operation} changed Activity.Current before awaiting the command.");
            }

            if (completedOnReturn != completeSynchronously)
            {
                throw new InvalidOperationException($"{operation} did not use the expected task completion path.");
            }

            if (!ReferenceEquals(parent, Activity.Current))
            {
                throw new InvalidOperationException($"{operation} changed Activity.Current after awaiting the command.");
            }

            using var subsequent = source.StartActivity($"{operation}.subsequent")
                                   ?? throw new InvalidOperationException("The subsequent activity was not created.");
            if (subsequent.TraceId != parent.TraceId || subsequent.ParentSpanId != parent.SpanId)
            {
                throw new InvalidOperationException($"{operation} broke the subsequent activity's parent context.");
            }
        }
    }
}

#endif

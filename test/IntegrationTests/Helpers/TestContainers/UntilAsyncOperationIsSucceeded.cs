// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace IntegrationTests.Helpers.TestContainers;

internal sealed class UntilAsyncOperationIsSucceeded : IWaitUntil
{
    private readonly int _maxCallCount;
    private readonly Func<Task<bool>> _operation;
    private int _tryCount;

    public UntilAsyncOperationIsSucceeded(Func<Task<bool>> operation, int maxCallCount)
    {
        _operation = operation;
        _maxCallCount = maxCallCount;
    }

    public async Task<bool> UntilAsync(IContainer container)
    {
        if (++_tryCount > _maxCallCount)
        {
            throw new TimeoutException($"Number of failed operations exceeded max count ({_maxCallCount}).");
        }

        return await _operation().ConfigureAwait(false);
    }
}

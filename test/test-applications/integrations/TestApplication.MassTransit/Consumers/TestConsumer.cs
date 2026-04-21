// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;
using TestApplication.MassTransit.Contracts;

namespace TestApplication.MassTransit.Consumers;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
#pragma warning disable CA1515 // Consider making public types internal. It is public because MassTransit needs to access it.
public sealed class TestConsumer :
#pragma warning restore CA1515 // Consider making public types internal. It is public because MassTransit needs to access it.
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
    IConsumer<TestMessage>
{
    private readonly ILogger<TestConsumer> _logger;

    public TestConsumer(ILogger<TestConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TestMessage> context)
    {
#if NET
        ArgumentNullException.ThrowIfNull(context);
#else
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
#endif

        _logger.LogReceivedText(context.Message.Value);
        return Task.CompletedTask;
    }
}

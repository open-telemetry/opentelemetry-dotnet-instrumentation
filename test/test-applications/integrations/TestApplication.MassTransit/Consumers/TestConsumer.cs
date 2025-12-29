// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;
using TestApplication.MassTransit.Contracts;

namespace TestApplication.MassTransit.Consumers;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
internal sealed class TestConsumer :
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
        _logger.LogReceivedText(context.Message.Value);
        return Task.CompletedTask;
    }
}

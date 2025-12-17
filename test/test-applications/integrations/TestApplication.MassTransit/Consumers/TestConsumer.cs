// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;
using TestApplication.MassTransit.Contracts;

namespace TestApplication.MassTransit.Consumers;

public class TestConsumer :
    IConsumer<TestMessage>
{
    private readonly ILogger<TestConsumer> _logger;

    public TestConsumer(ILogger<TestConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TestMessage> context)
    {
        _logger.LogInformation("Received Text: {Text}", context.Message.Value);
        return Task.CompletedTask;
    }
}

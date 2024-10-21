// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;

namespace TestApplication.MassTransit.Consumers;

public class TestConsumerDefinition : ConsumerDefinition<TestConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;

namespace TestApplication.MassTransit.Consumers;

public class TestConsumerDefinition :
    ConsumerDefinition<TestConsumer>
{
#if MASSTRANSIT_8_1_OR_GREATER
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TestConsumer> consumerConfigurator, IRegistrationContext context)
#else
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TestConsumer> consumerConfigurator)
#endif
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
    }
}

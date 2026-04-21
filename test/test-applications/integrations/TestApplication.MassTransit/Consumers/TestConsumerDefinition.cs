// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;

namespace TestApplication.MassTransit.Consumers;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
internal sealed class TestConsumerDefinition : ConsumerDefinition<TestConsumer>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TestConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
    }
}

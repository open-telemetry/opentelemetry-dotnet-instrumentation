// <copyright file="TestConsumerDefinition.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
